using DailyPlanner.Data;
using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Services;

public sealed partial class PlannerService
{
    private readonly IDbContextFactory<PlannerDbContext> _dbFactory;

    /// <summary>
    /// Constructor-injected DbContext factory. Each call to <c>_dbFactory.CreateDbContext()</c>
    /// produces a fresh, scoped DbContext — matches the pattern PlannerService partials
    /// have always used (open, work, dispose) but routed through DI instead of the
    /// static <c>_dbFactory.CreateDbContext()</c> which carried a mutable
    /// <c>OverrideFactory</c> field that race-conditioned parallel test fixtures.
    /// </summary>
    public PlannerService(IDbContextFactory<PlannerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PlannerWeek> GetOrCreateWeekAsync(DateOnly startDate, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();

        // Split query: 5 nested Includes produce a Cartesian explosion in
        // single-query mode (~thousands of rows for one week). AsSplitQuery
        // emits one query per collection, much smaller transferred payload.
        var week = await db.Weeks
            .AsSplitQuery()
            .Include(w => w.Goals.OrderBy(g => g.Order))
            .Include(w => w.Days.OrderBy(d => d.Date))
                .ThenInclude(d => d.Tasks.OrderBy(t => t.Order))
                    .ThenInclude(t => t.SubTasks.OrderBy(s => s.Order))
            .Include(w => w.Days)
                .ThenInclude(d => d.State)
            .Include(w => w.Habits.OrderBy(h => h.Order))
                .ThenInclude(h => h.Entries)
            .Include(w => w.WeeklyNotes.OrderBy(n => n.Order))
            .FirstOrDefaultAsync(w => w.StartDate == startDate, ct).ConfigureAwait(false);

        if (week is not null)
        {
            // Ensure all days have a DailyState (may be null for weeks created before v2.13)
            var missingStates = week.Days.Where(d => d.State is null).ToList();
            if (missingStates.Count > 0)
            {
                foreach (var day in missingStates)
                    day.State = new DailyState();
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            return week;
        }

        week = new PlannerWeek { StartDate = startDate };

        for (var i = 1; i <= 4; i++)
            week.Goals.Add(new WeeklyGoal { Order = i });

        for (var i = 0; i < 7; i++)
        {
            var day = new DailyPlan
            {
                Date = startDate.AddDays(i),
                State = new DailyState(),
                Tasks = Enumerable.Range(1, 10)
                    .Select(o => new DailyTask { Order = o })
                    .ToList()
            };
            week.Days.Add(day);
        }

        var prevWeek = await db.Weeks
            .Include(w => w.Habits)
            .Where(w => w.StartDate < startDate)
            .OrderByDescending(w => w.StartDate)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        var prevHabits = prevWeek?.Habits
            .Where(h => !string.IsNullOrWhiteSpace(h.Name))
            .OrderBy(h => h.Order)
            .ToList() ?? [];

        // Brand-new install seeds 5 empty slots; any subsequent week mirrors
        // exactly what the user keeps from the previous week — deleted habits
        // must stay deleted.
        var habitCount = prevWeek is null ? 5 : prevHabits.Count;
        for (var i = 1; i <= habitCount; i++)
        {
            var habit = new HabitDefinition
            {
                Order = i,
                Name = i <= prevHabits.Count ? prevHabits[i - 1].Name : string.Empty
            };
            for (var d = DayOfWeek.Monday; d <= DayOfWeek.Saturday; d++)
                habit.Entries.Add(new HabitEntry { DayOfWeek = d });
            habit.Entries.Add(new HabitEntry { DayOfWeek = DayOfWeek.Sunday });
            week.Habits.Add(habit);
        }

        db.Weeks.Add(week);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return week;
    }


    public async Task SaveTaskAsync(DailyTask task, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        db.DailyTasks.Update(task);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task AddSubTaskAsync(DailyTask subTask, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        db.DailyTasks.Add(subTask);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // Undo for delete: tries to fill an empty slot in the original day so the
    // restored task doesn't collide with whatever the user added after the delete.
    public async Task RestoreTaskAsync(DailyTask snapshot, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var emptySlot = await db.DailyTasks
            .Where(t => t.DailyPlanId == snapshot.DailyPlanId && t.ParentTaskId == null && (t.Text == null || t.Text == ""))
            .OrderBy(t => t.Order)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        if (emptySlot is not null)
        {
            emptySlot.Text = snapshot.Text;
            emptySlot.IsCompleted = snapshot.IsCompleted;
            emptySlot.Priority = snapshot.Priority;
            emptySlot.Category = snapshot.Category;
            emptySlot.Deadline = snapshot.Deadline;
            emptySlot.ReminderTime = snapshot.ReminderTime;
            emptySlot.ExternalId = snapshot.ExternalId;
        }
        else
        {
            var maxOrder = await db.DailyTasks
                .Where(t => t.DailyPlanId == snapshot.DailyPlanId)
                .Select(t => (int?)t.Order)
                .MaxAsync(ct).ConfigureAwait(false) ?? 0;
            db.DailyTasks.Add(new DailyTask
            {
                DailyPlanId = snapshot.DailyPlanId,
                Order = maxOrder + 1,
                Text = snapshot.Text,
                IsCompleted = snapshot.IsCompleted,
                Priority = snapshot.Priority,
                Category = snapshot.Category,
                Deadline = snapshot.Deadline,
                ReminderTime = snapshot.ReminderTime,
                ExternalId = snapshot.ExternalId
            });
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveSubTaskAsync(int subTaskId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var task = await db.DailyTasks.FindAsync([subTaskId], ct).ConfigureAwait(false);
        if (task is not null)
        {
            db.DailyTasks.Remove(task);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task RemoveTaskAsync(int taskId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var task = await db.DailyTasks.Include(t => t.SubTasks).FirstOrDefaultAsync(t => t.Id == taskId, ct).ConfigureAwait(false);
        if (task is null) return;

        if (task.SubTasks.Count > 0)
            db.DailyTasks.RemoveRange(task.SubTasks);
        db.DailyTasks.Remove(task);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task MoveTaskToNextDayAsync(int taskId, DateOnly targetDate, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var task = await db.DailyTasks.Include(t => t.SubTasks).FirstOrDefaultAsync(t => t.Id == taskId, ct).ConfigureAwait(false);
        if (task is null) return;

        var targetDay = await db.DailyPlans.Include(d => d.Tasks)
            .FirstOrDefaultAsync(d => d.Date == targetDate, ct).ConfigureAwait(false);
        if (targetDay is null) return;

        // Try to fill an empty slot first (so the task doesn't land at the bottom)
        var emptySlot = targetDay.Tasks
            .Where(t => string.IsNullOrWhiteSpace(t.Text) && t.ParentTaskId is null)
            .OrderBy(t => t.Order)
            .FirstOrDefault();

        DailyTask targetTask;
        if (emptySlot is not null)
        {
            emptySlot.Text = task.Text;
            emptySlot.Priority = task.Priority;
            emptySlot.Category = task.Category;
            emptySlot.IsCompleted = task.IsCompleted;
            emptySlot.Deadline = task.Deadline;
            emptySlot.ReminderTime = task.ReminderTime;
            emptySlot.ExternalId = task.ExternalId;
            targetTask = emptySlot;
        }
        else
        {
            var nextOrder = targetDay.Tasks.Count > 0 ? targetDay.Tasks.Max(t => t.Order) + 1 : 1;
            targetTask = new DailyTask
            {
                DailyPlanId = targetDay.Id,
                Order = nextOrder,
                Text = task.Text,
                Priority = task.Priority,
                Category = task.Category,
                IsCompleted = task.IsCompleted,
                Deadline = task.Deadline,
                ReminderTime = task.ReminderTime,
                ExternalId = task.ExternalId
            };
            db.DailyTasks.Add(targetTask);
        }
        // The Trello link now lives on the copy; clearing the source BEFORE the
        // flush keeps ExternalId unique per table at every intermediate state.
        task.ExternalId = null;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (var sub in task.SubTasks.OrderBy(s => s.Order))
        {
            db.DailyTasks.Add(new DailyTask
            {
                DailyPlanId = targetDay.Id,
                ParentTaskId = targetTask.Id,
                Order = sub.Order,
                Text = sub.Text,
                Priority = sub.Priority,
                Category = sub.Category,
                IsCompleted = sub.IsCompleted,
                Deadline = sub.Deadline,
                ReminderTime = sub.ReminderTime
            });
        }

        // Remove original
        if (task.SubTasks.Count > 0)
            db.DailyTasks.RemoveRange(task.SubTasks);
        db.DailyTasks.Remove(task);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task AddGoalAsync(WeeklyGoal goal, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        db.WeeklyGoals.Add(goal);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveGoalAsync(WeeklyGoal goal, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        db.WeeklyGoals.Update(goal);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveGoalAsync(int goalId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var goal = await db.WeeklyGoals.FindAsync([goalId], ct).ConfigureAwait(false);
        if (goal is not null) { db.WeeklyGoals.Remove(goal); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }

    public async Task SaveDailyStateAsync(DailyState state, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (state.Id == 0)
            db.DailyStates.Add(state);
        else
            db.DailyStates.Update(state);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveHabitEntryAsync(HabitEntry entry, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        db.HabitEntries.Update(entry);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task AddHabitAsync(HabitDefinition habit, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        db.HabitDefinitions.Add(habit);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveHabitDefinitionAsync(HabitDefinition habit, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        db.HabitDefinitions.Update(habit);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveHabitAsync(HabitDefinition habit, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var existing = await db.HabitDefinitions
            .Include(h => h.Entries)
            .FirstOrDefaultAsync(h => h.Id == habit.Id, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            db.HabitEntries.RemoveRange(existing.Entries);
            db.HabitDefinitions.Remove(existing);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task SaveNotesAsync(int weekId, string notes, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var week = await db.Weeks.FindAsync([weekId], ct).ConfigureAwait(false);
        if (week is not null)
        {
            week.Notes = notes;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<List<RecurringTemplate>> GetActiveTemplatesAsync(CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.RecurringTemplates.Where(t => t.IsActive).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveTemplateAsync(RecurringTemplate template, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (template.Id == 0)
            db.RecurringTemplates.Add(template);
        else
            db.RecurringTemplates.Update(template);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveTemplateAsync(int templateId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var t = await db.RecurringTemplates.FindAsync([templateId], ct).ConfigureAwait(false);
        if (t is not null) { db.RecurringTemplates.Remove(t); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }

    public async Task ApplyTemplatesAsync(PlannerWeek week, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var templates = await db.RecurringTemplates.Where(t => t.IsActive && !string.IsNullOrEmpty(t.Text)).ToListAsync(ct).ConfigureAwait(false);
        if (templates.Count == 0) return;

        var modifiedTasks = new List<DailyTask>();
        foreach (var day in week.Days)
        {
            foreach (var template in templates)
            {
                if (template.DayOfWeek is not null && template.DayOfWeek != day.Date.DayOfWeek)
                    continue;

                var alreadyExists = day.Tasks.Any(t => t.Text == template.Text);
                if (alreadyExists) continue;

                // ParentTaskId filter: never let a template occupy an empty subtask row.
                var emptySlot = day.Tasks.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text) && t.ParentTaskId is null);
                if (emptySlot is not null)
                {
                    emptySlot.Text = template.Text;
                    emptySlot.Priority = template.Priority;
                    emptySlot.Category = template.Category;
                    modifiedTasks.Add(emptySlot);
                }
            }
        }

        if (modifiedTasks.Count > 0)
        {
            foreach (var task in modifiedTasks)
                db.DailyTasks.Update(task);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task SaveWeeklyNoteAsync(WeeklyNote note, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (note.Id == 0)
            db.WeeklyNotes.Add(note);
        else
            db.WeeklyNotes.Update(note);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveWeeklyNoteAsync(int noteId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var note = await db.WeeklyNotes.FindAsync([noteId], ct).ConfigureAwait(false);
        if (note is not null) { db.WeeklyNotes.Remove(note); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }

    public async Task SaveReminderAsync(Reminder reminder, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (reminder.Id == 0)
            db.Reminders.Add(reminder);
        else
            db.Reminders.Update(reminder);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveReminderAsync(int reminderId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var r = await db.Reminders.FindAsync([reminderId], ct).ConfigureAwait(false);
        if (r is not null) { db.Reminders.Remove(r); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }

    public async Task<List<Reminder>> GetRemindersAsync(CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.Reminders.AsNoTracking().OrderBy(r => r.Time).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveMeetingAsync(Meeting meeting, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (meeting.Id == 0)
            db.Meetings.Add(meeting);
        else
            db.Meetings.Update(meeting);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveMeetingAsync(int meetingId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var m = await db.Meetings.FindAsync([meetingId], ct).ConfigureAwait(false);
        if (m is not null) { db.Meetings.Remove(m); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }

    public async Task<List<Meeting>> GetMeetingsAsync(CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.Meetings.AsNoTracking().OrderBy(m => m.DateTime).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task CopyWeekStructureAsync(int sourceWeekId, int targetWeekId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var source = await db.Weeks
            .Include(w => w.Days).ThenInclude(d => d.Tasks)
            .FirstOrDefaultAsync(w => w.Id == sourceWeekId, ct).ConfigureAwait(false);
        var target = await db.Weeks
            .Include(w => w.Days).ThenInclude(d => d.Tasks)
            .FirstOrDefaultAsync(w => w.Id == targetWeekId, ct).ConfigureAwait(false);
        if (source is null || target is null) return;

        var sourceDays = source.Days.OrderBy(d => d.Date).ToList();
        var targetDays = target.Days.OrderBy(d => d.Date).ToList();

        for (var i = 0; i < Math.Min(sourceDays.Count, targetDays.Count); i++)
        {
            var sTasks = sourceDays[i].Tasks.Where(t => !string.IsNullOrWhiteSpace(t.Text)).OrderBy(t => t.Order).ToList();
            foreach (var sTask in sTasks)
            {
                // ParentTaskId filter: an empty SUBTASK row must never be used as a
                // top-level slot — the copied text would silently become a subtask.
                var slot = targetDays[i].Tasks.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text) && t.ParentTaskId is null);
                if (slot is not null)
                {
                    slot.Text = sTask.Text;
                    slot.Priority = sTask.Priority;
                    slot.Category = sTask.Category;
                }
            }
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task CarryOverTasksAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var fromDay = await db.DailyPlans.Include(d => d.Tasks).ThenInclude(t => t.SubTasks).FirstOrDefaultAsync(d => d.Date == fromDate, ct).ConfigureAwait(false);
        var toDay = await db.DailyPlans.Include(d => d.Tasks).ThenInclude(t => t.SubTasks).FirstOrDefaultAsync(d => d.Date == toDate, ct).ConfigureAwait(false);
        if (fromDay is null || toDay is null) return;

        var incomplete = fromDay.Tasks
            .Where(t => !t.IsCompleted && !string.IsNullOrWhiteSpace(t.Text) && t.ParentTaskId is null)
            .ToList();
        var nextOrder = toDay.Tasks.Count > 0 ? toDay.Tasks.Max(t => t.Order) + 1 : 1;

        // source -> carried copy, by reference. Re-linking subtasks through a
        // Text lookup used to attach them to the wrong parent when two carried
        // tasks shared the same text.
        var carryMap = new Dictionary<DailyTask, DailyTask>();

        foreach (var task in incomplete)
        {
            DailyTask target;
            var emptySlot = toDay.Tasks.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text) && t.ParentTaskId is null);
            if (emptySlot is not null)
            {
                emptySlot.Text = task.Text;
                emptySlot.Priority = task.Priority;
                emptySlot.Category = task.Category;
                emptySlot.IsCompleted = false;
                emptySlot.Deadline = task.Deadline;
                emptySlot.ReminderTime = task.ReminderTime;
                emptySlot.ExternalId = task.ExternalId;
                target = emptySlot;
            }
            else
            {
                target = new DailyTask
                {
                    Order = nextOrder++,
                    Text = task.Text,
                    Priority = task.Priority,
                    Category = task.Category,
                    Deadline = task.Deadline,
                    ReminderTime = task.ReminderTime,
                    ExternalId = task.ExternalId
                };
                toDay.Tasks.Add(target);
            }
            // Trello dedup: the carried copy is the live task now — the stale
            // source must not keep the card id (known footgun: dropping it here
            // made SyncTrelloAsync re-import the card as a duplicate).
            task.ExternalId = null;
            carryMap[task] = target;
        }

        // Flush so every new/updated parent task gets a real Id
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Carry over subtasks (batch — one SaveChanges for all)
        foreach (var (task, target) in carryMap)
        {
            foreach (var sub in task.SubTasks.Where(s => !s.IsCompleted && !string.IsNullOrWhiteSpace(s.Text)).OrderBy(s => s.Order))
            {
                db.DailyTasks.Add(new DailyTask
                {
                    DailyPlanId = toDay.Id,
                    ParentTaskId = target.Id,
                    Order = sub.Order,
                    Text = sub.Text,
                    Priority = sub.Priority,
                    Category = sub.Category,
                    IsCompleted = false,
                    Deadline = sub.Deadline,
                    ReminderTime = sub.ReminderTime
                });
            }
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<PlannerWeek>> GetWeeksInRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        // Same shape as GetOrCreateWeekAsync: 4 sibling Include chains explode
        // into a ~10k-row cartesian per week without AsSplitQuery. Statistics only
        // reads, so AsNoTracking skips snapshotting hundreds of entities per load.
        return await db.Weeks
            .AsNoTracking()
            .AsSplitQuery()
            .Include(w => w.Days).ThenInclude(d => d.Tasks).ThenInclude(t => t.SubTasks)
            .Include(w => w.Days).ThenInclude(d => d.State)
            .Include(w => w.Goals)
            .Include(w => w.Habits).ThenInclude(h => h.Entries)
            .Where(w => w.StartDate >= from && w.StartDate <= to)
            .OrderBy(w => w.StartDate)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public static DateOnly GetWeekStart(DateOnly date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff);
    }

    // ─── Finance: Categories ────────────────────────────────────────








    // ─── Finance: Entries ─────────────────────────────────────────




    // ─── Finance: Budgets ─────────────────────────────────────────




    // ─── Finance: Debts ───────────────────────────────────────────






    // ─── Finance: Recurring Payments ──────────────────────────────





    public static List<DateOnly> GetWeekStartsForMonth(int year, int month)
    {
        var starts = new List<DateOnly>();
        var first = new DateOnly(year, month, 1);
        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var weekStart = GetWeekStart(first);

        // Add all weeks that overlap with this month
        while (weekStart <= last)
        {
            starts.Add(weekStart);
            weekStart = weekStart.AddDays(7);
        }

        return [.. starts.Distinct().OrderBy(d => d)];
    }

    // ─── Finance: Analytics ─────────────────────────────────────────


    // ─── Financial Goals ──────────────────────────────────────────




    // ─── Accounts ────────────────────────────────────────────────





    // ─── Account Transfers ───────────────────────────────────────




    // ─── Split Entries ───────────────────────────────────────────


    // ─── Forecast ────────────────────────────────────────────────



    // ─── Cashflow Calendar ───────────────────────────────────────


    // ─── Import ──────────────────────────────────────────────────


    // ─── Income Sources ───────────────────────────────────────────







    // ─── Inbox ─────────────────────────────────────────────────────





    // ─── Trello Settings & Sync ────────────────────────────────────



}

public sealed record CategoryBreakdownItem(int CategoryId, string Name, string Icon, string Color, decimal Amount);
public sealed record MonthlyFinanceSummary(int Year, int Month, decimal Income, decimal Expenses)
{
    public decimal Balance => Income - Expenses;
    public string Label => $"{Loc.GetMonthName(Month)[..3]} {Year % 100:D2}";
}
public sealed record ForecastDay(DateOnly Date, decimal Income, decimal Expenses, decimal Balance)
{
    public decimal NetFlow => Income - Expenses;
    public string Label => Date.ToString("dd.MM");
}
public sealed record CashflowDay(DateOnly Date, decimal Income, decimal Expenses)
{
    public decimal Net => Income - Expenses;
    public bool HasActivity => Income > 0 || Expenses > 0;
    public string DayLabel => Date.Day.ToString();
}
public sealed record IncomePaymentStatus(int PaymentId, int DayOfMonth, decimal Amount, string Description, DateOnly PayDate, bool IsPaid)
{
    public string DisplayDate => PayDate.ToString("dd.MM");
}
public sealed record IncomeSourceStatus(
    int SourceId, string Name, string ClientName, string Icon, string Color,
    decimal TotalMonthly, decimal Expected, decimal Received,
    List<IncomePaymentStatus> Payments)
{
    public decimal Remaining => Expected - Received;
    public double ProgressPercent => Expected > 0 ? (double)(Received / Expected) * 100 : 0;
}
