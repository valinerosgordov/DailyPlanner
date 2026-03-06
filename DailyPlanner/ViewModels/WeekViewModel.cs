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

    [ObservableProperty]
    private string _notes;

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
                $"Задачи: {CompletedTasks}/{TotalTasks}",
                $"Цели: {CompletedGoals}/{Goals.Count}",
                $"Лучший день: {MostProductiveDay}"
            };
            if (AverageProgress >= 80) parts.Add("\u2b50 Отличная неделя!");
            else if (AverageProgress >= 50) parts.Add("\u2705 Хорошая неделя!");
            else if (TotalTasks > 0) parts.Add("\U0001f4aa Можно лучше!");
            return string.Join("  \u00b7  ", parts);
        }
    }

    partial void OnNotesChanged(string value)
    {
        _model.Notes = value;
        DebounceService.Debounce($"notes-{_model.Id}",
            () => _service.SaveNotesAsync(_model.Id, value));
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
    }
}
