using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class StatisticsViewModel : ObservableObject
{
    private readonly PlannerService _service;

    [ObservableProperty] private int _selectedYear = DateTime.Today.Year;
    [ObservableProperty] private int _selectedMonth = DateTime.Today.Month;
    [ObservableProperty] private string _periodLabel = string.Empty;

    // Monthly stats
    [ObservableProperty] private int _monthTotalTasks;
    [ObservableProperty] private int _monthCompletedTasks;
    [ObservableProperty] private double _monthCompletionRate;
    [ObservableProperty] private int _monthTotalGoals;
    [ObservableProperty] private int _monthCompletedGoals;
    [ObservableProperty] private string _mostProductiveDay = "-";
    [ObservableProperty] private double _avgSleep;
    [ObservableProperty] private double _avgEnergy;
    [ObservableProperty] private double _avgMood;

    // Week comparison
    [ObservableProperty] private int _currentWeekTasks;
    [ObservableProperty] private int _previousWeekTasks;
    [ObservableProperty] private int _currentWeekCompleted;
    [ObservableProperty] private int _previousWeekCompleted;
    [ObservableProperty] private string _weekTrend = string.Empty;

    // Habit data
    public ObservableCollection<HabitHeatmapItem> HeatmapData { get; } = [];
    public ObservableCollection<HabitStreakItem> Streaks { get; } = [];

    // Weekly bars for chart
    public ObservableCollection<WeekBarItem> WeeklyBars { get; } = [];

    public StatisticsViewModel(PlannerService service)
    {
        _service = service;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        var firstDay = new DateOnly(SelectedYear, SelectedMonth, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var weekStart = PlannerService.GetWeekStart(firstDay);
        var weekEnd = PlannerService.GetWeekStart(lastDay).AddDays(6);

        PeriodLabel = $"{GetMonthName(SelectedMonth)} {SelectedYear}";

        var weeks = await _service.GetWeeksInRangeAsync(weekStart, weekEnd);

        // Monthly aggregation
        var allDays = weeks.SelectMany(w => w.Days)
            .Where(d => d.Date.Month == SelectedMonth && d.Date.Year == SelectedYear).ToList();
        var allTasks = allDays.SelectMany(d => d.Tasks).Where(t => !string.IsNullOrWhiteSpace(t.Text)).ToList();
        var allGoals = weeks.SelectMany(w => w.Goals).Where(g => !string.IsNullOrWhiteSpace(g.Text)).ToList();

        MonthTotalTasks = allTasks.Count;
        MonthCompletedTasks = allTasks.Count(t => t.IsCompleted);
        MonthCompletionRate = MonthTotalTasks > 0 ? Math.Round((double)MonthCompletedTasks / MonthTotalTasks * 100, 1) : 0;
        MonthTotalGoals = allGoals.Count;
        MonthCompletedGoals = allGoals.Count(g => g.IsCompleted);

        // Most productive day of week
        var dayGroups = allDays.GroupBy(d => d.Date.DayOfWeek)
            .Select(g => new { Day = g.Key, Completed = g.Sum(d => d.Tasks.Count(t => t.IsCompleted)) })
            .OrderByDescending(g => g.Completed).FirstOrDefault();
        MostProductiveDay = dayGroups is not null ? GetDayName(dayGroups.Day) : "-";

        // Averages
        var states = allDays.Where(d => d.State is not null).Select(d => d.State!).ToList();
        AvgSleep = states.Count > 0 ? Math.Round(states.Average(s => s.Sleep), 1) : 0;
        AvgEnergy = states.Count > 0 ? Math.Round(states.Average(s => s.Energy), 1) : 0;
        AvgMood = states.Count > 0 ? Math.Round(states.Average(s => s.Mood), 1) : 0;

        // Weekly bars
        WeeklyBars.Clear();
        foreach (var week in weeks.OrderBy(w => w.StartDate))
        {
            var wTasks = week.Days.SelectMany(d => d.Tasks).Where(t => !string.IsNullOrWhiteSpace(t.Text)).ToList();
            WeeklyBars.Add(new WeekBarItem(
                $"{week.StartDate:dd.MM}",
                wTasks.Count,
                wTasks.Count(t => t.IsCompleted)));
        }

        // Week comparison
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentStart = PlannerService.GetWeekStart(today);
        var prevStart = currentStart.AddDays(-7);
        var currentWeek = weeks.FirstOrDefault(w => w.StartDate == currentStart);
        var prevWeek = weeks.FirstOrDefault(w => w.StartDate == prevStart);

        if (currentWeek is not null)
        {
            var ct = currentWeek.Days.SelectMany(d => d.Tasks).Where(t => !string.IsNullOrWhiteSpace(t.Text)).ToList();
            CurrentWeekTasks = ct.Count;
            CurrentWeekCompleted = ct.Count(t => t.IsCompleted);
        }
        if (prevWeek is not null)
        {
            var pt = prevWeek.Days.SelectMany(d => d.Tasks).Where(t => !string.IsNullOrWhiteSpace(t.Text)).ToList();
            PreviousWeekTasks = pt.Count;
            PreviousWeekCompleted = pt.Count(t => t.IsCompleted);
        }

        var diff = CurrentWeekCompleted - PreviousWeekCompleted;
        WeekTrend = diff > 0 ? $"+{diff} задач" : diff < 0 ? $"{diff} задач" : "Без изменений";

        // Habit heatmap (last 30 days)
        HeatmapData.Clear();
        var allHabits = weeks.SelectMany(w => w.Habits).ToList();
        for (var d = today.AddDays(-29); d <= today; d = d.AddDays(1))
        {
            var dayOfWeek = d.DayOfWeek;
            var weekForDay = weeks.FirstOrDefault(w => w.StartDate <= d && w.StartDate.AddDays(6) >= d);
            if (weekForDay is null) { HeatmapData.Add(new HabitHeatmapItem(d, 0, 0)); continue; }

            var entries = weekForDay.Habits.SelectMany(h => h.Entries)
                .Where(e => e.DayOfWeek == dayOfWeek).ToList();
            HeatmapData.Add(new HabitHeatmapItem(d, entries.Count(e => e.IsCompleted), entries.Count));
        }

        // Habit streaks
        Streaks.Clear();
        var currentWeekHabits = weeks.FirstOrDefault(w => w.StartDate == currentStart)?.Habits ?? [];
        foreach (var habit in currentWeekHabits.Where(h => !string.IsNullOrWhiteSpace(h.Name)))
        {
            var streak = CalculateStreak(habit, weeks);
            Streaks.Add(new HabitStreakItem(habit.Name, streak));
        }
    }

    private static int CalculateStreak(HabitDefinition habit, List<PlannerWeek> weeks)
    {
        var streak = 0;
        var today = DateOnly.FromDateTime(DateTime.Today);
        for (var d = today; d >= today.AddDays(-60); d = d.AddDays(-1))
        {
            var week = weeks.FirstOrDefault(w => w.StartDate <= d && w.StartDate.AddDays(6) >= d);
            var matchHabit = week?.Habits.FirstOrDefault(h => h.Name == habit.Name);
            var entry = matchHabit?.Entries.FirstOrDefault(e => e.DayOfWeek == d.DayOfWeek);
            if (entry?.IsCompleted == true) streak++;
            else break;
        }
        return streak;
    }

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        if (SelectedMonth == 1) { SelectedMonth = 12; SelectedYear--; }
        else SelectedMonth--;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        if (SelectedMonth == 12) { SelectedMonth = 1; SelectedYear++; }
        else SelectedMonth++;
        await LoadDataAsync();
    }

    private static string GetMonthName(int m) => m switch
    {
        1 => "Январь", 2 => "Февраль", 3 => "Март", 4 => "Апрель",
        5 => "Май", 6 => "Июнь", 7 => "Июль", 8 => "Август",
        9 => "Сентябрь", 10 => "Октябрь", 11 => "Ноябрь", 12 => "Декабрь", _ => ""
    };

    private static string GetDayName(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => "Понедельник", DayOfWeek.Tuesday => "Вторник",
        DayOfWeek.Wednesday => "Среда", DayOfWeek.Thursday => "Четверг",
        DayOfWeek.Friday => "Пятница", DayOfWeek.Saturday => "Суббота",
        DayOfWeek.Sunday => "Воскресенье", _ => ""
    };
}

public sealed record WeekBarItem(string Label, int Total, int Completed)
{
    public double CompletionPercent => Total > 0 ? (double)Completed / Total * 100 : 0;
}

public sealed record HabitHeatmapItem(DateOnly Date, int Completed, int Total)
{
    public double Intensity => Total > 0 ? (double)Completed / Total : 0;
    public string DateLabel => Date.ToString("dd.MM");
}

public sealed record HabitStreakItem(string Name, int Streak)
{
    public string StreakText => Streak > 0 ? $"{Streak} дн." : "—";
}
