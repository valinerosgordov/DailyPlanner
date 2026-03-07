using DailyPlanner.Data;
using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Services;

public sealed class PlannerService
{
    public async Task<PlannerWeek> GetOrCreateWeekAsync(DateOnly startDate, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();

        var week = await db.Weeks
            .Include(w => w.Goals.OrderBy(g => g.Order))
            .Include(w => w.Days.OrderBy(d => d.Date))
                .ThenInclude(d => d.Tasks.OrderBy(t => t.Order))
                    .ThenInclude(t => t.SubTasks.OrderBy(s => s.Order))
            .Include(w => w.Days)
                .ThenInclude(d => d.State)
            .Include(w => w.Habits.OrderBy(h => h.Order))
                .ThenInclude(h => h.Entries)
            .Include(w => w.WeeklyNotes.OrderBy(n => n.Order))
            .FirstOrDefaultAsync(w => w.StartDate == startDate, ct);

        if (week is not null)
            return week;

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

        // Copy habit names from previous week, or create empty ones
        var prevWeek = await db.Weeks
            .Include(w => w.Habits)
            .Where(w => w.StartDate < startDate)
            .OrderByDescending(w => w.StartDate)
            .FirstOrDefaultAsync(ct);

        var prevHabits = prevWeek?.Habits
            .Where(h => !string.IsNullOrWhiteSpace(h.Name))
            .OrderBy(h => h.Order)
            .ToList() ?? [];

        var habitCount = Math.Max(prevHabits.Count, 5);
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
        await db.SaveChangesAsync(ct);

        return week;
    }

    public async Task SaveChangesAsync(PlannerWeek week, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.Weeks.Update(week);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveTaskAsync(DailyTask task, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.DailyTasks.Update(task);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddSubTaskAsync(DailyTask subTask, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.DailyTasks.Add(subTask);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveSubTaskAsync(int subTaskId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var task = await db.DailyTasks.FindAsync([subTaskId], ct);
        if (task is not null)
        {
            db.DailyTasks.Remove(task);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task SaveGoalAsync(WeeklyGoal goal, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.WeeklyGoals.Update(goal);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveDailyStateAsync(DailyState state, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.DailyStates.Update(state);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveHabitEntryAsync(HabitEntry entry, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.HabitEntries.Update(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveHabitDefinitionAsync(HabitDefinition habit, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.HabitDefinitions.Update(habit);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveHabitAsync(HabitDefinition habit, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var existing = await db.HabitDefinitions
            .Include(h => h.Entries)
            .FirstOrDefaultAsync(h => h.Id == habit.Id, ct);
        if (existing is not null)
        {
            db.HabitEntries.RemoveRange(existing.Entries);
            db.HabitDefinitions.Remove(existing);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task SaveNotesAsync(int weekId, string notes, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var week = await db.Weeks.FindAsync([weekId], ct);
        if (week is not null)
        {
            week.Notes = notes;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<RecurringTemplate>> GetActiveTemplatesAsync(CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        return await db.RecurringTemplates.Where(t => t.IsActive).ToListAsync(ct);
    }

    public async Task SaveTemplateAsync(RecurringTemplate template, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (template.Id == 0)
            db.RecurringTemplates.Add(template);
        else
            db.RecurringTemplates.Update(template);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveTemplateAsync(int templateId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var t = await db.RecurringTemplates.FindAsync([templateId], ct);
        if (t is not null) { db.RecurringTemplates.Remove(t); await db.SaveChangesAsync(ct); }
    }

    public async Task ApplyTemplatesAsync(PlannerWeek week, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var templates = await db.RecurringTemplates.Where(t => t.IsActive && !string.IsNullOrEmpty(t.Text)).ToListAsync(ct);
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

                var emptySlot = day.Tasks.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text));
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
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task SaveWeeklyNoteAsync(WeeklyNote note, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (note.Id == 0)
            db.WeeklyNotes.Add(note);
        else
            db.WeeklyNotes.Update(note);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveWeeklyNoteAsync(int noteId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var note = await db.WeeklyNotes.FindAsync([noteId], ct);
        if (note is not null) { db.WeeklyNotes.Remove(note); await db.SaveChangesAsync(ct); }
    }

    public async Task SaveReminderAsync(Reminder reminder, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (reminder.Id == 0)
            db.Reminders.Add(reminder);
        else
            db.Reminders.Update(reminder);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveReminderAsync(int reminderId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var r = await db.Reminders.FindAsync([reminderId], ct);
        if (r is not null) { db.Reminders.Remove(r); await db.SaveChangesAsync(ct); }
    }

    public async Task<List<Reminder>> GetRemindersAsync(CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        return await db.Reminders.OrderBy(r => r.Time).ToListAsync(ct);
    }

    public async Task CopyWeekStructureAsync(int sourceWeekId, int targetWeekId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var source = await db.Weeks
            .Include(w => w.Days).ThenInclude(d => d.Tasks)
            .FirstOrDefaultAsync(w => w.Id == sourceWeekId, ct);
        var target = await db.Weeks
            .Include(w => w.Days).ThenInclude(d => d.Tasks)
            .FirstOrDefaultAsync(w => w.Id == targetWeekId, ct);
        if (source is null || target is null) return;

        var sourceDays = source.Days.OrderBy(d => d.Date).ToList();
        var targetDays = target.Days.OrderBy(d => d.Date).ToList();

        for (var i = 0; i < Math.Min(sourceDays.Count, targetDays.Count); i++)
        {
            var sTasks = sourceDays[i].Tasks.Where(t => !string.IsNullOrWhiteSpace(t.Text)).OrderBy(t => t.Order).ToList();
            foreach (var sTask in sTasks)
            {
                var slot = targetDays[i].Tasks.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text));
                if (slot is not null)
                {
                    slot.Text = sTask.Text;
                    slot.Priority = sTask.Priority;
                    slot.Category = sTask.Category;
                }
            }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task CarryOverTasksAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var fromDay = await db.DailyPlans.Include(d => d.Tasks).ThenInclude(t => t.SubTasks).FirstOrDefaultAsync(d => d.Date == fromDate, ct);
        var toDay = await db.DailyPlans.Include(d => d.Tasks).ThenInclude(t => t.SubTasks).FirstOrDefaultAsync(d => d.Date == toDate, ct);
        if (fromDay is null || toDay is null) return;

        var incomplete = fromDay.Tasks.Where(t => !t.IsCompleted && !string.IsNullOrWhiteSpace(t.Text)).ToList();
        var nextOrder = toDay.Tasks.Count > 0 ? toDay.Tasks.Max(t => t.Order) + 1 : 1;
        foreach (var task in incomplete)
        {
            var emptySlot = toDay.Tasks.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text));
            if (emptySlot is not null)
            {
                emptySlot.Text = task.Text;
                emptySlot.Priority = task.Priority;
                emptySlot.Category = task.Category;
            }
            else
            {
                toDay.Tasks.Add(new DailyTask
                {
                    Order = nextOrder++, Text = task.Text,
                    Priority = task.Priority, Category = task.Category
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<PlannerWeek>> GetWeeksInRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        return await db.Weeks
            .Include(w => w.Days).ThenInclude(d => d.Tasks).ThenInclude(t => t.SubTasks)
            .Include(w => w.Days).ThenInclude(d => d.State)
            .Include(w => w.Goals)
            .Include(w => w.Habits).ThenInclude(h => h.Entries)
            .Where(w => w.StartDate >= from && w.StartDate <= to)
            .OrderBy(w => w.StartDate)
            .ToListAsync(ct);
    }

    public static DateOnly GetWeekStart(DateOnly date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff);
    }

    public static List<DateOnly> GetWeekStartsForMonth(int year, int month)
    {
        var starts = new List<DateOnly>();
        var first = new DateOnly(year, month, 1);
        var weekStart = GetWeekStart(first);

        while (weekStart.Month <= month && weekStart.Year <= year
               || weekStart < first)
        {
            starts.Add(weekStart);
            weekStart = weekStart.AddDays(7);
            if (weekStart.Month > month && weekStart.Year >= year)
                break;
        }

        // Include weeks that start in prev month but overlap this month
        var check = GetWeekStart(first);
        if (!starts.Contains(check))
            starts.Insert(0, check);

        return [.. starts.Distinct().OrderBy(d => d)];
    }
}
