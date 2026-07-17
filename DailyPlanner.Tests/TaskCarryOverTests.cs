using DailyPlanner.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Tests;

/// <summary>
/// Regression gate for the known footgun from CLAUDE.md: every path that moves
/// or carries a task MUST bring Deadline / ReminderTime / ExternalId along,
/// and exactly one row may own a Trello ExternalId afterwards.
/// </summary>
public class TaskCarryOverTests : PlannerServiceTestFixture
{
    private static readonly DateOnly Monday = new(2026, 4, 13);

    [Fact]
    public async Task MoveTaskToNextDay_CarriesDeadlineReminderAndExternalId()
    {
        var week = await Service.GetOrCreateWeekAsync(Monday);
        var source = week.Days[0].Tasks[0];
        source.Text = "Trello card";
        source.Deadline = new DateOnly(2026, 4, 20);
        source.ReminderTime = new TimeOnly(9, 30);
        source.ExternalId = "trello-abc";
        await Service.SaveTaskAsync(source);

        await Service.MoveTaskToNextDayAsync(source.Id, week.Days[1].Date);

        var reloaded = await Service.GetOrCreateWeekAsync(Monday);
        var moved = reloaded.Days[1].Tasks.Single(t => t.Text == "Trello card");
        moved.Deadline.Should().Be(new DateOnly(2026, 4, 20));
        moved.ReminderTime.Should().Be(new TimeOnly(9, 30));
        moved.ExternalId.Should().Be("trello-abc");

        // Exactly one row owns the Trello link after the move
        await using var db = CreateContext();
        (await db.DailyTasks.CountAsync(t => t.ExternalId == "trello-abc")).Should().Be(1);
    }

    [Fact]
    public async Task CarryOver_CarriesFieldsAndTransfersExternalIdOffTheSource()
    {
        var week = await Service.GetOrCreateWeekAsync(Monday);
        var source = week.Days[0].Tasks[0];
        source.Text = "Unfinished";
        source.Deadline = new DateOnly(2026, 4, 21);
        source.ReminderTime = new TimeOnly(14, 0);
        source.ExternalId = "trello-xyz";
        await Service.SaveTaskAsync(source);

        await Service.CarryOverTasksAsync(week.Days[0].Date, week.Days[1].Date);

        var reloaded = await Service.GetOrCreateWeekAsync(Monday);
        var carried = reloaded.Days[1].Tasks.Single(t => t.Text == "Unfinished");
        carried.Deadline.Should().Be(new DateOnly(2026, 4, 21), "carry-over must not silently drop the deadline");
        carried.ReminderTime.Should().Be(new TimeOnly(14, 0), "carry-over must not silently drop the reminder");
        carried.ExternalId.Should().Be("trello-xyz", "otherwise Trello sync re-imports the card as a duplicate");

        var stale = reloaded.Days[0].Tasks.Single(t => t.Id == source.Id);
        stale.ExternalId.Should().BeNull("the carried copy is the live task — the stale source must release the card id");
    }

    [Fact]
    public async Task CarryOver_DuplicateTexts_RelinksSubtasksToTheirOwnParents()
    {
        var week = await Service.GetOrCreateWeekAsync(Monday);
        var day0 = week.Days[0];

        var parentA = day0.Tasks[0];
        parentA.Text = "Same title";
        await Service.SaveTaskAsync(parentA);
        await Service.AddSubTaskAsync(new DailyTask
        {
            DailyPlanId = day0.Id, ParentTaskId = parentA.Id, Order = 1, Text = "subA"
        });

        var parentB = day0.Tasks[1];
        parentB.Text = "Same title";
        await Service.SaveTaskAsync(parentB);
        await Service.AddSubTaskAsync(new DailyTask
        {
            DailyPlanId = day0.Id, ParentTaskId = parentB.Id, Order = 1, Text = "subB"
        });

        await Service.CarryOverTasksAsync(day0.Date, week.Days[1].Date);

        var reloaded = await Service.GetOrCreateWeekAsync(Monday);
        var parents = reloaded.Days[1].Tasks
            .Where(t => t.Text == "Same title" && t.ParentTaskId is null)
            .ToList();
        parents.Should().HaveCount(2);
        // Text-keyed re-linking used to pile both subtasks onto the first parent
        parents.Should().OnlyContain(p => p.SubTasks.Count == 1,
            "each carried parent must receive its own subtask");
        parents.SelectMany(p => p.SubTasks).Select(s => s.Text)
            .Should().BeEquivalentTo(["subA", "subB"]);
    }

    [Fact]
    public async Task ApplyTemplates_NeverOccupiesAnEmptySubtaskRow()
    {
        var week = await Service.GetOrCreateWeekAsync(Monday);
        var day0 = week.Days[0];
        var parent = day0.Tasks[0];
        parent.Text = "Parent";
        await Service.SaveTaskAsync(parent);
        // Empty-text subtask row — the trap the slot search used to fall into
        await Service.AddSubTaskAsync(new DailyTask
        {
            DailyPlanId = day0.Id, ParentTaskId = parent.Id, Order = 1, Text = ""
        });

        await Service.SaveTemplateAsync(new RecurringTemplate { Text = "Daily standup", IsActive = true });
        var fresh = await Service.GetOrCreateWeekAsync(Monday);
        await Service.ApplyTemplatesAsync(fresh);

        await using var db = CreateContext();
        var templateTasks = await db.DailyTasks.Where(t => t.Text == "Daily standup").ToListAsync();
        templateTasks.Should().NotBeEmpty();
        templateTasks.Should().OnlyContain(t => t.ParentTaskId == null,
            "a template must fill a top-level slot, never an empty subtask row");
    }
}
