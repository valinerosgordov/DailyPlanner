using DailyPlanner.Models;
using DailyPlanner.Services;
using FluentAssertions;

namespace DailyPlanner.Tests;

public class TrelloTwoWayTests : PlannerServiceTestFixture
{
    [Fact]
    public async Task TrelloSettings_PushCompletions_RoundTrips()
    {
        var settings = await Service.GetTrelloSettingsAsync();
        settings.PushCompletions = true;
        settings.IsEnabled = true;
        await Service.SaveTrelloSettingsAsync(settings);

        var reloaded = await Service.GetTrelloSettingsAsync();
        reloaded.PushCompletions.Should().BeTrue();
    }

    [Fact]
    public async Task PushCompletedToTrello_Disabled_DoesNothing()
    {
        // PushCompletions defaults to false (opt-in) — the push pass must be a
        // no-op and, critically, must not touch the network at all.
        var pushed = await Service.PushCompletedToTrelloAsync(new TrelloService());
        pushed.Should().Be(0);
    }

    [Fact]
    public async Task CompletedTrelloTask_IsQueuedForPush_UntilStamped()
    {
        var week = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        var task = week.Days[0].Tasks[0];
        task.Text = "Trello task";
        task.ExternalId = "card-1";
        task.IsCompleted = true;
        await Service.SaveTaskAsync(task);

        await using var db = CreateContext();
        var pending = db.DailyTasks
            .Where(t => t.ExternalId != null && t.IsCompleted && t.ExternalClosedUtc == null)
            .ToList();
        pending.Should().ContainSingle(t => t.ExternalId == "card-1",
            "a completed Trello task without a push stamp is exactly what the push pass picks up");
    }
}
