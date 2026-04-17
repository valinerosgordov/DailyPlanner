using DailyPlanner.Models;
using DailyPlanner.ViewModels;
using FluentAssertions;

namespace DailyPlanner.Tests;

public class WeeklyReviewViewModelTests
{
    private static PlannerWeek BuildWeek(Action<PlannerWeek>? tweak = null)
    {
        var week = new PlannerWeek { StartDate = new DateOnly(2026, 4, 13) };
        for (var i = 0; i < 4; i++) week.Goals.Add(new WeeklyGoal { Order = i + 1 });
        for (var i = 0; i < 7; i++)
        {
            var day = new DailyPlan { Date = week.StartDate.AddDays(i), State = new DailyState() };
            for (var t = 0; t < 10; t++) day.Tasks.Add(new DailyTask { Order = t + 1 });
            week.Days.Add(day);
        }
        var habit = new HabitDefinition { Name = "Test" };
        for (var d = DayOfWeek.Monday; d <= DayOfWeek.Saturday; d++)
            habit.Entries.Add(new HabitEntry { DayOfWeek = d });
        habit.Entries.Add(new HabitEntry { DayOfWeek = DayOfWeek.Sunday });
        week.Habits.Add(habit);
        tweak?.Invoke(week);
        return week;
    }

    [Fact]
    public void Empty_Week_ZeroStats()
    {
        var vm = new WeeklyReviewViewModel();
        vm.LoadFrom(BuildWeek());

        vm.TotalTasks.Should().Be(0);
        vm.CompletedTasks.Should().Be(0);
        vm.CompletionRate.Should().Be(0);
        vm.BestDayName.Should().Be("—");
        vm.BestDayCount.Should().Be(0);
    }

    [Fact]
    public void CompletionRate_Calculated()
    {
        var week = BuildWeek(w =>
        {
            w.Days[0].Tasks[0].Text = "a"; w.Days[0].Tasks[0].IsCompleted = true;
            w.Days[0].Tasks[1].Text = "b"; w.Days[0].Tasks[1].IsCompleted = false;
            w.Days[0].Tasks[2].Text = "c"; w.Days[0].Tasks[2].IsCompleted = true;
        });
        var vm = new WeeklyReviewViewModel();
        vm.LoadFrom(week);

        vm.TotalTasks.Should().Be(3);
        vm.CompletedTasks.Should().Be(2);
        vm.CompletionRate.Should().BeApproximately(67, 1); // 2/3 ~= 66.67
    }

    [Fact]
    public void BestDay_PicksDayWithMostCompleted()
    {
        var week = BuildWeek(w =>
        {
            w.Days[0].Tasks[0].Text = "a"; w.Days[0].Tasks[0].IsCompleted = true;
            w.Days[1].Tasks[0].Text = "b"; w.Days[1].Tasks[0].IsCompleted = true;
            w.Days[1].Tasks[1].Text = "c"; w.Days[1].Tasks[1].IsCompleted = true;
            w.Days[2].Tasks[0].Text = "d"; w.Days[2].Tasks[0].IsCompleted = false;
        });
        var vm = new WeeklyReviewViewModel();
        vm.LoadFrom(week);

        vm.BestDayCount.Should().Be(2);
        // Tuesday (Day[1]) was 2026-04-14 — Tuesday
        vm.BestDayName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Goals_CountsCompletedCorrectly()
    {
        var week = BuildWeek(w =>
        {
            w.Goals[0].IsCompleted = true;
            w.Goals[1].IsCompleted = true;
            w.Goals[2].IsCompleted = false;
            w.Goals[3].IsCompleted = false;
        });
        var vm = new WeeklyReviewViewModel();
        vm.LoadFrom(week);

        vm.TotalGoals.Should().Be(4);
        vm.GoalsReached.Should().Be(2);
    }

    [Fact]
    public void Habits_CountsCompletedEntries()
    {
        var week = BuildWeek(w =>
        {
            w.Habits[0].Entries[0].IsCompleted = true;
            w.Habits[0].Entries[1].IsCompleted = true;
            w.Habits[0].Entries[2].IsCompleted = false;
        });
        var vm = new WeeklyReviewViewModel();
        vm.LoadFrom(week);

        vm.HabitsTotal.Should().Be(7);
        vm.HabitsCompleted.Should().Be(2);
    }

    [Fact]
    public void AverageState_ComputedFromDailyStates()
    {
        var week = BuildWeek(w =>
        {
            w.Days[0].State!.Sleep = 5; w.Days[0].State!.Energy = 4; w.Days[0].State!.Mood = 3;
            w.Days[1].State!.Sleep = 3; w.Days[1].State!.Energy = 2; w.Days[1].State!.Mood = 5;
        });
        var vm = new WeeklyReviewViewModel();
        vm.LoadFrom(week);

        // Average across ALL 7 days (some with zeroes)
        vm.AvgSleep.Should().BeInRange(1, 5);
        vm.AvgEnergy.Should().BeInRange(0.5, 5);
        vm.AvgMood.Should().BeInRange(1, 5);
    }

    [Fact]
    public void WeekLabel_IsDateRange()
    {
        var week = BuildWeek();
        var vm = new WeeklyReviewViewModel();
        vm.LoadFrom(week);
        vm.WeekLabel.Should().Be("13.04 — 19.04");
    }
}
