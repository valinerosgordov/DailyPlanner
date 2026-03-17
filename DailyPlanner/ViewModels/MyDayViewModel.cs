using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class MyDayViewModel : ObservableObject
{
    public string TodayDate { get; }
    public List<string> YesterdayTasks { get; }
    public List<TaskDisplayItem> TodayTasks { get; }
    public List<string> OverdueTasks { get; }
    public string Summary { get; }
    public bool HasYesterdayTasks => YesterdayTasks.Count > 0;
    public bool HasNoTodayTasks => TodayTasks.Count == 0;
    public bool HasOverdueTasks => OverdueTasks.Count > 0;

    [ObservableProperty]
    private bool _dontShowAgain;

    public MyDayViewModel(WeekViewModel? week)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        TodayDate = today.ToString("dddd, dd MMMM yyyy");

        YesterdayTasks = [];
        TodayTasks = [];
        OverdueTasks = [];

        if (week is null)
        {
            Summary = Loc.Get("MyDayEmpty");
            return;
        }

        // Yesterday's incomplete
        var yesterday = week.Days.FirstOrDefault(d => d.Date == today.AddDays(-1));
        if (yesterday is not null)
        {
            YesterdayTasks = yesterday.Tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Text) && !t.IsCompleted)
                .Select(t => t.Text)
                .Take(8)
                .ToList();
        }

        // Today's tasks
        var todayDay = week.Days.FirstOrDefault(d => d.Date == today);
        if (todayDay is not null)
        {
            TodayTasks = todayDay.Tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                .Select(t => new TaskDisplayItem(
                    t.IsCompleted ? "\u2705" : "\u25CB",
                    t.Text,
                    t.IsCompleted
                        ? new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99))
                        : new SolidColorBrush(Color.FromRgb(0x58, 0x58, 0x78))
                ))
                .Take(10)
                .ToList();
        }

        // Overdue tasks (across all days)
        foreach (var day in week.Days)
        {
            foreach (var task in day.Tasks)
            {
                if (task.Deadline is not null && task.Deadline.Value < today
                    && !task.IsCompleted && !string.IsNullOrWhiteSpace(task.Text))
                {
                    OverdueTasks.Add($"{task.Text} ({task.Deadline.Value:dd.MM})");
                }
            }
        }

        // Summary
        var parts = new List<string>();
        if (todayDay is not null)
        {
            var total = todayDay.TotalWithText;
            var done = todayDay.CompletedCount;
            parts.Add(string.Format(Loc.Get("MyDaySummaryTasks"), total, done));
        }
        if (YesterdayTasks.Count > 0)
            parts.Add(string.Format(Loc.Get("MyDaySummaryIncomplete"), YesterdayTasks.Count));
        if (OverdueTasks.Count > 0)
            parts.Add(string.Format(Loc.Get("MyDaySummaryOverdue"), OverdueTasks.Count));

        Summary = parts.Count > 0 ? string.Join("  ·  ", parts) : Loc.Get("MyDayEmpty");
    }
}

public sealed record TaskDisplayItem(string Icon, string Text, Brush IconBrush);
