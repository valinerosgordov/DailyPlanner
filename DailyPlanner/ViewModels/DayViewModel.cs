using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class DayViewModel : ObservableObject
{
    private readonly DailyPlan _model;
    private readonly PlannerService _service;

    public DayViewModel(DailyPlan model, PlannerService service)
    {
        _model = model;
        _service = service;

        Tasks = new ObservableCollection<TaskViewModel>(
            model.Tasks.Where(t => t.ParentTaskId is null)
                .OrderBy(t => t.Order)
                .Select(t => new TaskViewModel(t, service)));

        if (model.State is not null)
        {
            _sleep = model.State.Sleep;
            _energy = model.State.Energy;
            _mood = model.State.Mood;
        }

        foreach (var task in Tasks)
            SubscribeTaskStats(task);
    }

    public DailyPlan Model => _model;
    public DateOnly Date => _model.Date;

    public string DayName => Loc.GetDayName(Date.DayOfWeek);
    public string ShortDayName => Loc.GetShortDayName(Date.DayOfWeek);
    public string DateFormatted => Date.ToString("dd.MM");
    public bool IsToday => Date == DateOnly.FromDateTime(DateTime.Today);

    public ObservableCollection<TaskViewModel> Tasks { get; }

    public int CompletedCount => AllTasksFlat.Count(t => t.IsCompleted && !string.IsNullOrWhiteSpace(t.Text));
    public int TotalWithText => AllTasksFlat.Count(t => !string.IsNullOrWhiteSpace(t.Text));
    public int NotCompletedCount => TotalWithText - CompletedCount;
    public double ProgressPercent => TotalWithText > 0 ? (double)CompletedCount / TotalWithText * 100 : 0;

    private IEnumerable<TaskViewModel> AllTasksFlat =>
        Tasks.SelectMany(t => t.SubTasks.Prepend(t));

    private void SubscribeTaskStats(TaskViewModel task)
    {
        task.PropertyChanged += (_, _) => NotifyStats();
        foreach (var sub in task.SubTasks)
            sub.PropertyChanged += (_, _) => NotifyStats();
        task.SubTasks.CollectionChanged += (_, _) => NotifyStats();
    }

    private void NotifyStats()
    {
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(TotalWithText));
        OnPropertyChanged(nameof(NotCompletedCount));
        OnPropertyChanged(nameof(ProgressPercent));
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(DayName));
        OnPropertyChanged(nameof(ShortDayName));
    }

    [ObservableProperty] private int _sleep;
    [ObservableProperty] private int _energy;
    [ObservableProperty] private int _mood;

    private DailyState EnsureState()
    {
        if (_model.State is null)
            _model.State = new DailyState { DailyPlanId = _model.Id };
        return _model.State;
    }

    partial void OnSleepChanged(int value)
    {
        var state = EnsureState();
        state.Sleep = value;
        _service.SaveDailyStateAsync(state).FireAndForget("state-save");
    }

    partial void OnEnergyChanged(int value)
    {
        var state = EnsureState();
        state.Energy = value;
        _service.SaveDailyStateAsync(state).FireAndForget("state-save");
    }

    partial void OnMoodChanged(int value)
    {
        var state = EnsureState();
        state.Mood = value;
        _service.SaveDailyStateAsync(state).FireAndForget("state-save");
    }
}
