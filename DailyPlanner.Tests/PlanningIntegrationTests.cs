using DailyPlanner.Models;
using FluentAssertions;

namespace DailyPlanner.Tests;

public class PlanningIntegrationTests : PlannerServiceTestFixture
{
    // ─── Habits ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddHabit_StoresWithDefaultEntries()
    {
        var week = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        var habit = new HabitDefinition { WeekId = week.Id, Order = 99, Name = "Читать 30 минут" };
        habit.Entries.Add(new HabitEntry { DayOfWeek = DayOfWeek.Monday, IsCompleted = true });
        await Service.AddHabitAsync(habit);

        var reloaded = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        reloaded.Habits.Should().ContainSingle(h => h.Name == "Читать 30 минут");
    }

    [Fact]
    public async Task SaveHabitEntry_UpdatesCompletion()
    {
        var week = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        var entry = week.Habits[0].Entries.First();
        entry.IsCompleted = true;
        await Service.SaveHabitEntryAsync(entry);

        var reloaded = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        reloaded.Habits[0].Entries.First(e => e.Id == entry.Id).IsCompleted.Should().BeTrue();
    }

    // ─── Goals ──────────────────────────────────────────────────────

    [Fact]
    public async Task SaveGoal_UpdatesText()
    {
        var week = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        var goal = week.Goals[0];
        goal.Text = "Завершить MVP";
        goal.IsCompleted = true;
        await Service.SaveGoalAsync(goal);

        var reloaded = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        var sameGoal = reloaded.Goals.First(g => g.Id == goal.Id);
        sameGoal.Text.Should().Be("Завершить MVP");
        sameGoal.IsCompleted.Should().BeTrue();
    }

    // ─── Templates ──────────────────────────────────────────────────

    [Fact]
    public async Task Template_AppliedOnlyToMatchingDayOfWeek()
    {
        await Service.SaveTemplateAsync(new RecurringTemplate
        {
            Text = "Утренняя пробежка",
            DayOfWeek = DayOfWeek.Monday,
            IsActive = true
        });

        var week = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        await Service.ApplyTemplatesAsync(week);

        var reloaded = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        var monday = reloaded.Days.First(d => d.Date.DayOfWeek == DayOfWeek.Monday);
        monday.Tasks.Should().Contain(t => t.Text == "Утренняя пробежка");
    }

    // ─── CarryOver ──────────────────────────────────────────────────

    [Fact]
    public async Task CarryOverTasks_MovesIncompleteText()
    {
        var week = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        var mon = week.Days[0];
        var tue = week.Days[1];

        var incomplete = mon.Tasks[0];
        incomplete.Text = "Доделать спеку";
        incomplete.IsCompleted = false;
        await Service.SaveTaskAsync(incomplete);

        var done = mon.Tasks[1];
        done.Text = "Готово";
        done.IsCompleted = true;
        await Service.SaveTaskAsync(done);

        await Service.CarryOverTasksAsync(mon.Date, tue.Date);

        var reloaded = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        reloaded.Days[1].Tasks.Should().Contain(t => t.Text == "Доделать спеку");
        // Completed stays on source
        reloaded.Days[0].Tasks.Should().Contain(t => t.Text == "Готово" && t.IsCompleted);
    }

    // ─── CopyWeekStructure ──────────────────────────────────────────

    [Fact]
    public async Task CopyWeekStructure_ClonesTaskTextIntoMatchingDay()
    {
        var src = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        src.Days[0].Tasks[0].Text = "Понедельник A";
        src.Days[0].Tasks[1].Text = "Понедельник B";
        await Service.SaveTaskAsync(src.Days[0].Tasks[0]);
        await Service.SaveTaskAsync(src.Days[0].Tasks[1]);

        var dst = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 20));
        await Service.CopyWeekStructureAsync(src.Id, dst.Id);

        var dstReloaded = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 20));
        dstReloaded.Days[0].Tasks.Should().Contain(t => t.Text == "Понедельник A");
        dstReloaded.Days[0].Tasks.Should().Contain(t => t.Text == "Понедельник B");
    }

    // ─── WeeklyNotes / Reminders / Meetings ─────────────────────────

    [Fact]
    public async Task SaveWeeklyNote_Persists()
    {
        var week = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        var note = new WeeklyNote { WeekId = week.Id, Order = 1, Text = "Важно!" };
        await Service.SaveWeeklyNoteAsync(note);

        var reloaded = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        reloaded.WeeklyNotes.Should().Contain(n => n.Text == "Важно!");
    }

    [Fact]
    public async Task SaveReminder_AndRetrieve()
    {
        var reminder = new Reminder { Title = "Встреча", Message = "В 10:00", Time = new TimeOnly(10, 0) };
        await Service.SaveReminderAsync(reminder);
        (await Service.GetRemindersAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task SaveMeeting_AndRetrieve()
    {
        var meeting = new Meeting { Title = "Стендап", DateTime = DateTime.Today.AddHours(10) };
        await Service.SaveMeetingAsync(meeting);
        (await Service.GetMeetingsAsync()).Should().ContainSingle();
    }

    // ─── Income sources ─────────────────────────────────────────────

    [Fact]
    public async Task SaveIncomeSource_Persists()
    {
        var src = new IncomeSource
        {
            Name = "ProjectA", ClientName = "Client X", TotalMonthlyAmount = 5000m,
            Icon = "💰", Color = "#4ADE80"
        };
        await Service.SaveIncomeSourceAsync(src);

        var list = await Service.GetIncomeSourcesAsync();
        list.Should().ContainSingle(s => s.Name == "ProjectA");
    }
}
