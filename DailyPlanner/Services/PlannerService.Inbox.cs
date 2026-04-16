using DailyPlanner.Data;
using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Services;

public sealed partial class PlannerService
{
    public async Task<List<InboxTask>> GetInboxTasksAsync(CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        return await db.InboxTasks
            .Where(t => !t.IsArchived)
            .OrderByDescending(t => t.CreatedDate)
            .ThenByDescending(t => t.Id)
            .ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveInboxTaskAsync(InboxTask task, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (task.Id == 0)
            db.InboxTasks.Add(task);
        else
            db.InboxTasks.Update(task);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveInboxTaskAsync(int taskId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var t = await db.InboxTasks.FindAsync([taskId], ct).ConfigureAwait(false);
        if (t is null) return;
        db.InboxTasks.Remove(t);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task<DailyTask> MoveInboxToDayAsync(int inboxTaskId, DateOnly date, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var inbox = await db.InboxTasks.FindAsync([inboxTaskId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inbox task not found");

        var weekStart = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        var week = await db.Weeks.FirstOrDefaultAsync(w => w.StartDate == weekStart, ct).ConfigureAwait(false);
        if (week is null)
        {
            week = new PlannerWeek { StartDate = weekStart };
            db.Weeks.Add(week);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var plan = await db.DailyPlans.FirstOrDefaultAsync(p => p.WeekId == week.Id && p.Date == date, ct).ConfigureAwait(false);
        if (plan is null)
        {
            plan = new DailyPlan { WeekId = week.Id, Date = date };
            db.DailyPlans.Add(plan);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        // Try to fill the first empty slot (Text = empty, no parent) — avoids stacking
        // Trello tasks below pre-populated empty rows.
        var emptySlot = await db.DailyTasks
            .Where(t => t.DailyPlanId == plan.Id && t.ParentTaskId == null && (t.Text == null || t.Text == ""))
            .OrderBy(t => t.Order)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        DailyTask target;
        if (emptySlot is not null)
        {
            emptySlot.Text = inbox.Text;
            emptySlot.Priority = inbox.Priority;
            emptySlot.Category = inbox.Category;
            emptySlot.Deadline = inbox.DueDate;
            target = emptySlot;
        }
        else
        {
            var maxOrder = await db.DailyTasks.Where(t => t.DailyPlanId == plan.Id).Select(t => (int?)t.Order).MaxAsync(ct).ConfigureAwait(false) ?? 0;
            target = new DailyTask
            {
                DailyPlanId = plan.Id,
                Order = maxOrder + 1,
                Text = inbox.Text,
                Priority = inbox.Priority,
                Category = inbox.Category,
                Deadline = inbox.DueDate
            };
            db.DailyTasks.Add(target);
        }

        // Archive Trello-sourced inbox tasks instead of deleting so SyncTrelloAsync
        // knows this card was already placed and won't re-add it on the next sync.
        if (inbox.Source == InboxSource.Trello && !string.IsNullOrEmpty(inbox.ExternalId))
        {
            inbox.IsArchived = true;
            db.InboxTasks.Update(inbox);
        }
        else
        {
            db.InboxTasks.Remove(inbox);
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return target;
    }
    public async Task<TrelloSettings> GetTrelloSettingsAsync(CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var settings = await db.TrelloSettings.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (settings is null)
        {
            settings = new TrelloSettings();
            db.TrelloSettings.Add(settings);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        return settings;
    }
    public async Task SaveTrelloSettingsAsync(TrelloSettings settings, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (settings.Id == 0)
            db.TrelloSettings.Add(settings);
        else
            db.TrelloSettings.Update(settings);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task<int> SyncTrelloAsync(TrelloService trello, CancellationToken ct = default)
    {
        var settings = await GetTrelloSettingsAsync(ct).ConfigureAwait(false);
        if (!settings.IsEnabled || string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.Token))
            return 0;

        var cards = await trello.GetCardsInListByNameAsync(settings.ListName, settings.ApiKey, settings.Token, ct).ConfigureAwait(false);

        await using var db = PlannerDbContextFactory.Create();
        var existingExternalIds = await db.InboxTasks
            .Where(t => t.Source == InboxSource.Trello && t.ExternalId != null)
            .Select(t => t.ExternalId!)
            .ToListAsync(ct).ConfigureAwait(false);
        var existingSet = existingExternalIds.ToHashSet();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var added = 0;
        foreach (var (card, boardName, listName) in cards)
        {
            if (existingSet.Contains(card.Id)) continue;
            db.InboxTasks.Add(new InboxTask
            {
                Text = card.Name,
                Source = InboxSource.Trello,
                ExternalId = card.Id,
                BoardName = boardName,
                ListName = listName,
                Url = card.ShortUrl,
                CreatedDate = today,
                DueDate = card.Due.HasValue ? DateOnly.FromDateTime(card.Due.Value) : null
            });
            added++;
        }

        settings.LastSyncUtc = DateTime.UtcNow;
        db.TrelloSettings.Update(settings);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return added;
    }
}
