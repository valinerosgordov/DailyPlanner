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
            model.Tasks.OrderBy(t => t.Order).Select(t => new TaskViewModel(t, service)));

        if (model.State is not null)
        {
            _sleep = model.State.Sleep;
            _energy = model.State.Energy;
            _mood = model.State.Mood;
        }

        foreach (var task in Tasks)
            task.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(CompletedCount));
                OnPropertyChanged(nameof(TotalWithText));
                OnPropertyChanged(nameof(NotCompletedCount));
                OnPropertyChanged(nameof(ProgressPercent));
            };
    }

    public DailyPlan Model => _model;
    public DateOnly Date => _model.Date;

    public string DayName => Date.DayOfWeek switch
    {
        DayOfWeek.Monday => "Понедельник",
        DayOfWeek.Tuesday => "Вторник",
        DayOfWeek.Wednesday => "Среда",
        DayOfWeek.Thursday => "Четверг",
        DayOfWeek.Friday => "Пятница",
        DayOfWeek.Saturday => "Суббота",
        DayOfWeek.Sunday => "Воскресенье",
        _ => string.Empty
    };

    public string ShortDayName => Date.DayOfWeek switch
    {
        DayOfWeek.Monday => "Пн",
        DayOfWeek.Tuesday => "Вт",
        DayOfWeek.Wednesday => "Ср",
        DayOfWeek.Thursday => "Чт",
        DayOfWeek.Friday => "Пт",
        DayOfWeek.Saturday => "Сб",
        DayOfWeek.Sunday => "Вс",
        _ => string.Empty
    };

    public string DateFormatted => Date.ToString("dd.MM");

    public bool IsToday => Date == DateOnly.FromDateTime(DateTime.Today);

    public ObservableCollection<TaskViewModel> Tasks { get; }

    public int CompletedCount => Tasks.Count(t => t.IsCompleted && !string.IsNullOrWhiteSpace(t.Text));
    public int TotalWithText => Tasks.Count(t => !string.IsNullOrWhiteSpace(t.Text));
    public int NotCompletedCount => TotalWithText - CompletedCount;
    public double ProgressPercent => TotalWithText > 0 ? (double)CompletedCount / TotalWithText * 100 : 0;

    [ObservableProperty]
    private int _sleep;

    [ObservableProperty]
    private int _energy;

    [ObservableProperty]
    private int _mood;

    partial void OnSleepChanged(int value)
    {
        if (_model.State is null) return;
        _model.State.Sleep = value;
        _service.SaveDailyStateAsync(_model.State).FireAndForget("state-save");
    }

    partial void OnEnergyChanged(int value)
    {
        if (_model.State is null) return;
        _model.State.Energy = value;
        _service.SaveDailyStateAsync(_model.State).FireAndForget("state-save");
    }

    partial void OnMoodChanged(int value)
    {
        if (_model.State is null) return;
        _model.State.Mood = value;
        _service.SaveDailyStateAsync(_model.State).FireAndForget("state-save");
    }
}
