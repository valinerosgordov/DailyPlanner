using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class WeekViewModel : ObservableObject
{
    private readonly PlannerWeek _model;
    private readonly PlannerService _service;

    public WeekViewModel(PlannerWeek model, PlannerService service)
    {
        _model = model;
        _service = service;

        Goals = new ObservableCollection<GoalViewModel>(
            model.Goals.OrderBy(g => g.Order).Select(g => new GoalViewModel(g, service)));

        Days = new ObservableCollection<DayViewModel>(
            model.Days.OrderBy(d => d.Date).Select(d => new DayViewModel(d, service)));

        Habits = new ObservableCollection<HabitViewModel>(
            model.Habits.OrderBy(h => h.Order).Select(h => new HabitViewModel(h, service)));

        WeeklyNotes = new ObservableCollection<NoteViewModel>(
            model.WeeklyNotes.OrderBy(n => n.Order).Select(n => new NoteViewModel(n, service)));

        _notes = model.Notes;

        foreach (var day in Days)
            day.PropertyChanged += (_, _) => RefreshAnalytics();
        foreach (var goal in Goals)
            goal.PropertyChanged += (_, _) => RefreshAnalytics();
    }

    public PlannerWeek Model => _model;

    [ObservableProperty]
    private bool _isSelected;

    public DateOnly StartDate => _model.StartDate;
    public DateOnly EndDate => _model.StartDate.AddDays(6);
    public string WeekRange => $"{StartDate:dd.MM} - {EndDate:dd.MM}";

    public ObservableCollection<GoalViewModel> Goals { get; }
    public ObservableCollection<DayViewModel> Days { get; }
    public ObservableCollection<HabitViewModel> Habits { get; }
    public ObservableCollection<NoteViewModel> WeeklyNotes { get; }
    public bool HasNoHabits => Habits.Count == 0;

    [ObservableProperty]
    private string _notes;

    // State averages
    public double AvgSleep => Days.Any(d => d.Sleep > 0) ? Math.Round(Days.Where(d => d.Sleep > 0).Average(d => d.Sleep), 1) : 0;
    public double AvgEnergy => Days.Any(d => d.Energy > 0) ? Math.Round(Days.Where(d => d.Energy > 0).Average(d => d.Energy), 1) : 0;
    public double AvgMood => Days.Any(d => d.Mood > 0) ? Math.Round(Days.Where(d => d.Mood > 0).Average(d => d.Mood), 1) : 0;
    public bool HasStateData => Days.Any(d => d.Sleep > 0 || d.Energy > 0 || d.Mood > 0);

    public string StateInsight
    {
        get
        {
            if (!HasStateData) return Loc.Get("StateNoData");
            var best = Days.Where(d => d.Sleep + d.Energy + d.Mood > 0)
                .MaxBy(d => d.Sleep + d.Energy + d.Mood);
            var worst = Days.Where(d => d.Sleep + d.Energy + d.Mood > 0)
                .MinBy(d => d.Sleep + d.Energy + d.Mood);
            if (best is null) return "";
            var parts = new List<string>();
            parts.Add(string.Format(Loc.Get("StateBestDay"), best.ShortDayName));
            if (worst is not null && worst != best)
                parts.Add(string.Format(Loc.Get("StateWorstDay"), worst.ShortDayName));
            return string.Join("  ·  ", parts);
        }
    }

    // Analytics
    public int TotalTasks => Days.Sum(d => d.TotalWithText);
    public int CompletedTasks => Days.Sum(d => d.CompletedCount);
    public int NotCompletedTasks => TotalTasks - CompletedTasks;
    public double AverageProgress => TotalTasks > 0 ? Math.Round((double)CompletedTasks / TotalTasks * 100) : 0;
    public int CompletedGoals => Goals.Count(g => g.IsCompleted);

    public string MostProductiveDay
    {
        get
        {
            var best = Days.MaxBy(d => d.CompletedCount);
            return best?.ShortDayName ?? "-";
        }
    }

    public string WeeklySummary
    {
        get
        {
            var parts = new List<string>
            {
                string.Format(Loc.Get("SummaryTasks"), CompletedTasks, TotalTasks),
                string.Format(Loc.Get("SummaryGoals"), CompletedGoals, Goals.Count),
                string.Format(Loc.Get("SummaryBestDay"), MostProductiveDay)
            };
            if (AverageProgress >= 80) parts.Add(Loc.Get("SummaryExcellent"));
            else if (AverageProgress >= 50) parts.Add(Loc.Get("SummaryGood"));
            else if (TotalTasks > 0) parts.Add(Loc.Get("SummaryCanBetter"));
            return string.Join("  \u00b7  ", parts);
        }
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(MostProductiveDay));
        OnPropertyChanged(nameof(WeeklySummary));
    }

    partial void OnNotesChanged(string value)
    {
        _model.Notes = value;
        DebounceService.Debounce($"notes-{_model.Id}",
            () => _service.SaveNotesAsync(_model.Id, value));
    }

    [RelayCommand]
    private async Task AddGoalAsync()
    {
        var order = _model.Goals.Count + 1;
        var goal = new WeeklyGoal { WeekId = _model.Id, Order = order };
        _model.Goals.Add(goal);
        await _service.SaveChangesAsync(_model);

        var vm = new GoalViewModel(goal, _service);
        vm.PropertyChanged += (_, _) => RefreshAnalytics();
        Goals.Add(vm);
        OnPropertyChanged(nameof(CompletedGoals));
    }

    [RelayCommand]
    private async Task RemoveGoalAsync(GoalViewModel? goalVm)
    {
        if (goalVm is null) return;
        var model = _model.Goals.FirstOrDefault(g => g.Id == goalVm.Model.Id);
        if (model is not null)
        {
            _model.Goals.Remove(model);
            await _service.RemoveGoalAsync(model.Id);
        }
        Goals.Remove(goalVm);
        RefreshAnalytics();
    }

    [RelayCommand]
    private async Task AddHabitAsync()
    {
        var order = _model.Habits.Count + 1;
        var habit = new HabitDefinition { WeekId = _model.Id, Order = order };
        for (var d = DayOfWeek.Monday; d <= DayOfWeek.Saturday; d++)
            habit.Entries.Add(new HabitEntry { DayOfWeek = d });
        habit.Entries.Add(new HabitEntry { DayOfWeek = DayOfWeek.Sunday });

        _model.Habits.Add(habit);
        await _service.SaveChangesAsync(_model);

        var vm = new HabitViewModel(habit, _service);
        Habits.Add(vm);
        OnPropertyChanged(nameof(HasNoHabits));
    }

    [RelayCommand]
    private async Task AddNoteAsync()
    {
        var order = _model.WeeklyNotes.Count + 1;
        var note = new WeeklyNote { WeekId = _model.Id, Order = order };
        await _service.SaveWeeklyNoteAsync(note);
        _model.WeeklyNotes.Add(note);
        WeeklyNotes.Add(new NoteViewModel(note, _service));
    }

    [RelayCommand]
    private async Task RemoveNoteAsync(NoteViewModel? noteVm)
    {
        if (noteVm is null) return;
        var model = _model.WeeklyNotes.FirstOrDefault(n => n.Id == noteVm.Model.Id);
        if (model is not null)
        {
            _model.WeeklyNotes.Remove(model);
            await _service.RemoveWeeklyNoteAsync(model.Id);
        }
        WeeklyNotes.Remove(noteVm);
    }

    [RelayCommand]
    private async Task RemoveHabitAsync(HabitViewModel? habit)
    {
        if (habit is null) return;

        var model = _model.Habits.FirstOrDefault(h => h.Id == habit.Model.Id);
        if (model is not null)
        {
            _model.Habits.Remove(model);
            await _service.RemoveHabitAsync(model);
        }
        Habits.Remove(habit);
        OnPropertyChanged(nameof(HasNoHabits));
    }

    private void RefreshAnalytics()
    {
        OnPropertyChanged(nameof(TotalTasks));
        OnPropertyChanged(nameof(CompletedTasks));
        OnPropertyChanged(nameof(NotCompletedTasks));
        OnPropertyChanged(nameof(AverageProgress));
        OnPropertyChanged(nameof(CompletedGoals));
        OnPropertyChanged(nameof(MostProductiveDay));
        OnPropertyChanged(nameof(WeeklySummary));
        OnPropertyChanged(nameof(AvgSleep));
        OnPropertyChanged(nameof(AvgEnergy));
        OnPropertyChanged(nameof(AvgMood));
        OnPropertyChanged(nameof(HasStateData));
        OnPropertyChanged(nameof(StateInsight));
    }
}
