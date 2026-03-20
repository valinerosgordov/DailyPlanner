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
        {
            // Ensure all days have a DailyState (may be null for weeks created before v2.13)
            var missingStates = week.Days.Where(d => d.State is null).ToList();
            if (missingStates.Count > 0)
            {
                foreach (var day in missingStates)
                    day.State = new DailyState();
                await db.SaveChangesAsync(ct);
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

    public async Task RemoveTaskAsync(int taskId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var task = await db.DailyTasks.Include(t => t.SubTasks).FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null) return;

        if (task.SubTasks.Count > 0)
            db.DailyTasks.RemoveRange(task.SubTasks);
        db.DailyTasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }

    public async Task MoveTaskToNextDayAsync(int taskId, DateOnly targetDate, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var task = await db.DailyTasks.Include(t => t.SubTasks).FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null) return;

        var targetDay = await db.DailyPlans.Include(d => d.Tasks)
            .FirstOrDefaultAsync(d => d.Date == targetDate, ct);
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
                IsCompleted = task.IsCompleted
            };
            db.DailyTasks.Add(targetTask);
        }
        await db.SaveChangesAsync(ct);

        // Move subtasks (preserve all properties)
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
                IsCompleted = sub.IsCompleted
            });
        }

        // Remove original
        if (task.SubTasks.Count > 0)
            db.DailyTasks.RemoveRange(task.SubTasks);
        db.DailyTasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddGoalAsync(WeeklyGoal goal, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.WeeklyGoals.Add(goal);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveGoalAsync(WeeklyGoal goal, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.WeeklyGoals.Update(goal);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveGoalAsync(int goalId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var goal = await db.WeeklyGoals.FindAsync([goalId], ct);
        if (goal is not null) { db.WeeklyGoals.Remove(goal); await db.SaveChangesAsync(ct); }
    }

    public async Task SaveDailyStateAsync(DailyState state, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (state.Id == 0)
            db.DailyStates.Add(state);
        else
            db.DailyStates.Update(state);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveHabitEntryAsync(HabitEntry entry, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.HabitEntries.Update(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddHabitAsync(HabitDefinition habit, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.HabitDefinitions.Add(habit);
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

    public async Task SaveMeetingAsync(Meeting meeting, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (meeting.Id == 0)
            db.Meetings.Add(meeting);
        else
            db.Meetings.Update(meeting);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveMeetingAsync(int meetingId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var m = await db.Meetings.FindAsync([meetingId], ct);
        if (m is not null) { db.Meetings.Remove(m); await db.SaveChangesAsync(ct); }
    }

    public async Task<List<Meeting>> GetMeetingsAsync(CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        return await db.Meetings.OrderBy(m => m.DateTime).ToListAsync(ct);
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

        var incomplete = fromDay.Tasks
            .Where(t => !t.IsCompleted && !string.IsNullOrWhiteSpace(t.Text) && t.ParentTaskId is null)
            .ToList();
        var nextOrder = toDay.Tasks.Count > 0 ? toDay.Tasks.Max(t => t.Order) + 1 : 1;

        foreach (var task in incomplete)
        {
            DailyTask target;
            var emptySlot = toDay.Tasks.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text) && t.ParentTaskId is null);
            if (emptySlot is not null)
            {
                emptySlot.Text = task.Text;
                emptySlot.Priority = task.Priority;
                emptySlot.Category = task.Category;
                target = emptySlot;
            }
            else
            {
                target = new DailyTask
                {
                    Order = nextOrder++, Text = task.Text,
                    Priority = task.Priority, Category = task.Category
                };
                toDay.Tasks.Add(target);
            }

        }

        // Flush so every new/updated parent task gets a real Id
        await db.SaveChangesAsync(ct);

        // Carry over subtasks (batch — one SaveChanges for all)
        var targetLookup = toDay.Tasks
            .Where(t => t.ParentTaskId is null)
            .GroupBy(t => t.Text)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var task in incomplete)
        {
            if (!targetLookup.TryGetValue(task.Text, out var target)) continue;
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
                    IsCompleted = false
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

    // ─── Finance: Categories ────────────────────────────────────────

    public async Task<List<FinanceCategory>> GetFinanceCategoriesAsync(FinanceEntryType? type = null, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var query = db.FinanceCategories.Where(c => !c.IsArchived);
        if (type is not null) query = query.Where(c => c.Type == type);
        return await query.OrderBy(c => c.Order).ToListAsync(ct);
    }

    public async Task SaveFinanceCategoryAsync(FinanceCategory category, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (category.Id == 0)
            db.FinanceCategories.Add(category);
        else
            db.FinanceCategories.Update(category);
        await db.SaveChangesAsync(ct);
    }

    public async Task ArchiveFinanceCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var cat = await db.FinanceCategories.FindAsync([categoryId], ct);
        if (cat is not null)
        {
            cat.IsArchived = true;
            await db.SaveChangesAsync(ct);
        }
    }

    private static readonly Dictionary<string, string> SeedCategoryKeys = new()
    {
        ["Salary"] = "CatSalary", ["Freelance"] = "CatFreelance", ["Gifts"] = "CatGifts",
        ["Investments"] = "CatInvestments", ["Other Income"] = "CatOtherIncome",
        ["Food"] = "CatFood", ["Transport"] = "CatTransport", ["Housing"] = "CatHousing",
        ["Entertainment"] = "CatEntertainment", ["Health"] = "CatHealth",
        ["Clothing"] = "CatClothing", ["Subscriptions"] = "CatSubscriptions",
        ["Education"] = "CatEducation", ["Other Expense"] = "CatOtherExpense",
    };

    public async Task SeedFinanceCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();

        if (!await db.FinanceCategories.AnyAsync(ct))
        {
            var categories = new List<FinanceCategory>
            {
                new() { Name = Loc.Get("CatSalary"), Icon = "\uD83D\uDCBC", Color = "#34D399", Type = FinanceEntryType.Income, Order = 1 },
                new() { Name = Loc.Get("CatFreelance"), Icon = "\uD83D\uDCBB", Color = "#38BDF8", Type = FinanceEntryType.Income, Order = 2 },
                new() { Name = Loc.Get("CatGifts"), Icon = "\uD83C\uDF81", Color = "#A78BFA", Type = FinanceEntryType.Income, Order = 3 },
                new() { Name = Loc.Get("CatInvestments"), Icon = "\uD83D\uDCC8", Color = "#FBBF24", Type = FinanceEntryType.Income, Order = 4 },
                new() { Name = Loc.Get("CatOtherIncome"), Icon = "\u2795", Color = "#6EE7B7", Type = FinanceEntryType.Income, Order = 5 },
                new() { Name = Loc.Get("CatFood"), Icon = "\uD83C\uDF54", Color = "#FB923C", Type = FinanceEntryType.Expense, Order = 1 },
                new() { Name = Loc.Get("CatTransport"), Icon = "\uD83D\uDE97", Color = "#38BDF8", Type = FinanceEntryType.Expense, Order = 2 },
                new() { Name = Loc.Get("CatHousing"), Icon = "\uD83C\uDFE0", Color = "#A78BFA", Type = FinanceEntryType.Expense, Order = 3 },
                new() { Name = Loc.Get("CatEntertainment"), Icon = "\uD83C\uDFAE", Color = "#F472B6", Type = FinanceEntryType.Expense, Order = 4 },
                new() { Name = Loc.Get("CatHealth"), Icon = "\uD83D\uDC8A", Color = "#34D399", Type = FinanceEntryType.Expense, Order = 5 },
                new() { Name = Loc.Get("CatClothing"), Icon = "\uD83D\uDC55", Color = "#FBBF24", Type = FinanceEntryType.Expense, Order = 6 },
                new() { Name = Loc.Get("CatSubscriptions"), Icon = "\uD83D\uDCF1", Color = "#FB7185", Type = FinanceEntryType.Expense, Order = 7 },
                new() { Name = Loc.Get("CatEducation"), Icon = "\uD83D\uDCDA", Color = "#818CF8", Type = FinanceEntryType.Expense, Order = 8 },
                new() { Name = Loc.Get("CatOtherExpense"), Icon = "\u2796", Color = "#585878", Type = FinanceEntryType.Expense, Order = 9 },
            };

            db.FinanceCategories.AddRange(categories);
            await db.SaveChangesAsync(ct);
            return;
        }

        // Update existing seed category names to current language
        var allCats = await db.FinanceCategories.ToListAsync(ct);
        var changed = false;
        foreach (var cat in allCats)
        {
            // Check if name matches any known English seed name
            var key = SeedCategoryKeys.GetValueOrDefault(cat.Name);
            if (key is not null)
            {
                cat.Name = Loc.Get(key);
                changed = true;
            }
        }
        if (changed) await db.SaveChangesAsync(ct);
    }

    // ─── Finance: Entries ─────────────────────────────────────────

    public async Task<List<FinanceEntry>> GetFinanceEntriesAsync(DateOnly from, DateOnly to, FinanceEntryType? type = null, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var query = db.FinanceEntries.Include(e => e.Category).Where(e => e.Date >= from && e.Date <= to);
        if (type is not null) query = query.Where(e => e.Type == type);
        return await query.OrderBy(e => e.Date).ThenBy(e => e.Id).ToListAsync(ct);
    }

    public async Task SaveFinanceEntryAsync(FinanceEntry entry, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();

        // Prevent entries on archived categories
        if (entry.CategoryId > 0)
        {
            var cat = await db.FinanceCategories.FindAsync([entry.CategoryId], ct);
            if (cat is { IsArchived: true }) return;
        }

        // Detach navigation properties to avoid tracking conflicts
        var savedCategory = entry.Category;
        var savedWeek = entry.Week;
        var savedRecurring = entry.RecurringPayment;
        entry.Category = null;
        entry.Week = null;
        entry.RecurringPayment = null;

        if (entry.Id == 0)
            db.FinanceEntries.Add(entry);
        else
            db.FinanceEntries.Update(entry);
        await db.SaveChangesAsync(ct);

        entry.Category = savedCategory;
        entry.Week = savedWeek;
        entry.RecurringPayment = savedRecurring;
    }

    public async Task RemoveFinanceEntryAsync(int entryId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var entry = await db.FinanceEntries.FindAsync([entryId], ct);
        if (entry is not null) { db.FinanceEntries.Remove(entry); await db.SaveChangesAsync(ct); }
    }

    // ─── Finance: Budgets ─────────────────────────────────────────

    public async Task<List<FinanceBudget>> GetBudgetsAsync(string monthYear, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        return await db.FinanceBudgets.Include(b => b.Category).Where(b => b.MonthYear == monthYear).ToListAsync(ct);
    }

    public async Task SaveBudgetAsync(FinanceBudget budget, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var savedCategory = budget.Category;
        budget.Category = null;

        if (budget.Id == 0)
            db.FinanceBudgets.Add(budget);
        else
            db.FinanceBudgets.Update(budget);
        await db.SaveChangesAsync(ct);

        budget.Category = savedCategory;
    }

    public async Task RemoveBudgetAsync(int budgetId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var b = await db.FinanceBudgets.FindAsync([budgetId], ct);
        if (b is not null) { db.FinanceBudgets.Remove(b); await db.SaveChangesAsync(ct); }
    }

    // ─── Finance: Debts ───────────────────────────────────────────

    public async Task<List<Debt>> GetDebtsAsync(bool includeSettled = false, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var query = db.Debts.Include(d => d.Payments).AsQueryable();
        if (!includeSettled) query = query.Where(d => !d.IsSettled);
        return await query.OrderByDescending(d => d.CreatedDate).ToListAsync(ct);
    }

    public async Task SaveDebtAsync(Debt debt, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var savedPayments = debt.Payments;
        debt.Payments = [];

        if (debt.Id == 0)
            db.Debts.Add(debt);
        else
            db.Debts.Update(debt);
        await db.SaveChangesAsync(ct);

        debt.Payments = savedPayments;
    }

    public async Task RemoveDebtAsync(int debtId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var debt = await db.Debts.Include(d => d.Payments).FirstOrDefaultAsync(d => d.Id == debtId, ct);
        if (debt is not null)
        {
            db.DebtPayments.RemoveRange(debt.Payments);
            db.Debts.Remove(debt);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task AddDebtPaymentAsync(DebtPayment payment, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.DebtPayments.Add(payment);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveDebtPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var p = await db.DebtPayments.FindAsync([paymentId], ct);
        if (p is not null) { db.DebtPayments.Remove(p); await db.SaveChangesAsync(ct); }
    }

    // ─── Finance: Recurring Payments ──────────────────────────────

    public async Task<List<RecurringPayment>> GetRecurringPaymentsAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var query = db.RecurringPayments.Include(rp => rp.Category).AsQueryable();
        if (activeOnly) query = query.Where(rp => rp.IsActive);
        return await query.OrderBy(rp => rp.Type).ThenBy(rp => rp.Name).ToListAsync(ct);
    }

    public async Task SaveRecurringPaymentAsync(RecurringPayment payment, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var savedCategory = payment.Category;
        var savedEntries = payment.GeneratedEntries;
        payment.Category = null;
        payment.GeneratedEntries = [];

        if (payment.Id == 0)
            db.RecurringPayments.Add(payment);
        else
            db.RecurringPayments.Update(payment);
        await db.SaveChangesAsync(ct);

        payment.Category = savedCategory;
        payment.GeneratedEntries = savedEntries;
    }

    public async Task RemoveRecurringPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var rp = await db.RecurringPayments.FindAsync([paymentId], ct);
        if (rp is not null) { db.RecurringPayments.Remove(rp); await db.SaveChangesAsync(ct); }
    }

    public async Task GenerateRecurringEntriesAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var payments = await db.RecurringPayments
            .Where(rp => rp.IsActive && rp.AutoCreate && rp.StartDate <= to && (rp.EndDate == null || rp.EndDate >= from))
            .ToListAsync(ct);

        var existingKeys = (await db.FinanceEntries
            .Where(e => e.RecurringPaymentId != null && e.Date >= from && e.Date <= to)
            .Select(e => new { e.RecurringPaymentId, e.Date })
            .ToListAsync(ct))
            .ToHashSet();

        var newEntries = new List<FinanceEntry>();

        foreach (var rp in payments)
        {
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                if (date < rp.StartDate || (rp.EndDate is not null && date > rp.EndDate)) continue;

                var match = rp.Frequency switch
                {
                    PaymentFrequency.Monthly => rp.DayOfMonth is not null && date.Day == rp.DayOfMonth,
                    PaymentFrequency.Weekly => rp.DayOfWeek is not null && date.DayOfWeek == rp.DayOfWeek,
                    PaymentFrequency.Biweekly => rp.DayOfWeek is not null && date.DayOfWeek == rp.DayOfWeek
                        && Math.Abs(date.ToDateTime(TimeOnly.MinValue).Subtract(rp.StartDate.ToDateTime(TimeOnly.MinValue)).Days) % 14 == 0,
                    PaymentFrequency.Quarterly => rp.DayOfMonth is not null && date.Day == rp.DayOfMonth
                        && ((date.Year - rp.StartDate.Year) * 12 + date.Month - rp.StartDate.Month) % 3 == 0,
                    PaymentFrequency.Yearly => rp.DayOfMonth is not null && date.Day == rp.DayOfMonth
                        && date.Month == rp.StartDate.Month,
                    _ => false
                };

                if (!match) continue;
                if (!existingKeys.Add(new { RecurringPaymentId = (int?)rp.Id, Date = date })) continue;

                newEntries.Add(new FinanceEntry
                {
                    Date = date,
                    CategoryId = rp.CategoryId,
                    Type = rp.Type,
                    Amount = rp.Amount,
                    Description = rp.Name,
                    IsRecurring = true,
                    RecurringPaymentId = rp.Id,
                    IsPaid = false
                });
            }
        }

        if (newEntries.Count > 0)
        {
            db.FinanceEntries.AddRange(newEntries);
            await db.SaveChangesAsync(ct);
        }
    }

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

    public async Task<List<CategoryBreakdownItem>> GetExpensesByCategoryAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var entries = await db.FinanceEntries
            .Include(e => e.Category)
            .Where(e => e.Type == FinanceEntryType.Expense && e.Date >= from && e.Date <= to)
            .ToListAsync(ct);

        return entries
            .GroupBy(e => e.CategoryId)
            .Select(g =>
            {
                var cat = g.First().Category;
                return new CategoryBreakdownItem(
                    g.Key, cat?.Name ?? string.Empty, cat?.Icon ?? string.Empty,
                    cat?.Color ?? "#cba6f7", g.Sum(e => e.Amount));
            })
            .OrderByDescending(x => x.Amount)
            .ToList();
    }

    public async Task<List<MonthlyFinanceSummary>> GetMonthlyTotalsAsync(int months, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var cutoff = DateOnly.FromDateTime(DateTime.Today).AddMonths(-months + 1);
        cutoff = new DateOnly(cutoff.Year, cutoff.Month, 1);

        var rawEntries = await db.FinanceEntries
            .Where(e => e.Date >= cutoff)
            .Select(e => new { e.Date, e.Type, e.Amount })
            .ToListAsync(ct);

        var entries = rawEntries
            .GroupBy(e => new { e.Date.Year, e.Date.Month, e.Type })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Type, Total = g.Sum(e => e.Amount) })
            .ToList();

        var result = new List<MonthlyFinanceSummary>();
        for (var i = 0; i < months; i++)
        {
            var d = DateOnly.FromDateTime(DateTime.Today).AddMonths(-months + 1 + i);
            var y = d.Year;
            var m = d.Month;
            var inc = entries.FirstOrDefault(e => e.Year == y && e.Month == m && e.Type == FinanceEntryType.Income)?.Total ?? 0;
            var exp = entries.FirstOrDefault(e => e.Year == y && e.Month == m && e.Type == FinanceEntryType.Expense)?.Total ?? 0;
            result.Add(new MonthlyFinanceSummary(y, m, inc, exp));
        }

        return result;
    }
}

public sealed record CategoryBreakdownItem(int CategoryId, string Name, string Icon, string Color, decimal Amount);
public sealed record MonthlyFinanceSummary(int Year, int Month, decimal Income, decimal Expenses)
{
    public decimal Balance => Income - Expenses;
    public string Label => $"{Loc.GetMonthName(Month)[..3]} {Year % 100:D2}";
}
