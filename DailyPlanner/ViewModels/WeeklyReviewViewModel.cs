using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;

namespace DailyPlanner.ViewModels;

public sealed partial class WeeklyReviewViewModel : ObservableObject
{
    [ObservableProperty] private string _weekLabel = string.Empty;
    [ObservableProperty] private int _totalTasks;
    [ObservableProperty] private int _completedTasks;
    [ObservableProperty] private double _completionRate;
    [ObservableProperty] private string _bestDayName = string.Empty;
    [ObservableProperty] private int _bestDayCount;
    [ObservableProperty] private int _goalsReached;
    [ObservableProperty] private int _totalGoals;
    [ObservableProperty] private int _habitsTotal;
    [ObservableProperty] private int _habitsCompleted;
    [ObservableProperty] private double _avgSleep;
    [ObservableProperty] private double _avgEnergy;
    [ObservableProperty] private double _avgMood;

    public void LoadFrom(PlannerWeek week)
    {
        WeekLabel = $"{week.StartDate:dd.MM} — {week.StartDate.AddDays(6):dd.MM}";

        var allTasks = week.Days.SelectMany(d => d.Tasks)
            .Where(t => !string.IsNullOrWhiteSpace(t.Text))
            .ToList();
        TotalTasks = allTasks.Count;
        CompletedTasks = allTasks.Count(t => t.IsCompleted);
        CompletionRate = TotalTasks > 0 ? Math.Round((double)CompletedTasks / TotalTasks * 100, 0) : 0;

        var bestDay = week.Days
            .Select(d => new
            {
                d.Date,
                Completed = d.Tasks.Count(t => t.IsCompleted && !string.IsNullOrWhiteSpace(t.Text))
            })
            .OrderByDescending(x => x.Completed)
            .FirstOrDefault();

        if (bestDay is not null && bestDay.Completed > 0)
        {
            BestDayName = Services.Loc.GetDayName(bestDay.Date.DayOfWeek);
            BestDayCount = bestDay.Completed;
        }
        else
        {
            BestDayName = "—";
            BestDayCount = 0;
        }

        TotalGoals = week.Goals.Count;
        GoalsReached = week.Goals.Count(g => g.IsCompleted);

        HabitsTotal = week.Habits.Sum(h => h.Entries.Count);
        HabitsCompleted = week.Habits.Sum(h => h.Entries.Count(e => e.IsCompleted));

        var states = week.Days.Where(d => d.State is not null).Select(d => d.State!).ToList();
        AvgSleep = states.Count == 0 ? 0 : Math.Round(states.Average(s => (double)s.Sleep), 1);
        AvgEnergy = states.Count == 0 ? 0 : Math.Round(states.Average(s => (double)s.Energy), 1);
        AvgMood = states.Count == 0 ? 0 : Math.Round(states.Average(s => (double)s.Mood), 1);
    }
}
