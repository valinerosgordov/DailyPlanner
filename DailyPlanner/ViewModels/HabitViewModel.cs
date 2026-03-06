using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class HabitViewModel : ObservableObject
{
    private readonly HabitDefinition _model;
    private readonly PlannerService _service;

    public HabitViewModel(HabitDefinition model, PlannerService service)
    {
        _model = model;
        _service = service;
        _name = model.Name;

        Entries = new ObservableCollection<HabitEntryViewModel>(
            GetOrderedEntries().Select(e => new HabitEntryViewModel(e, service)));

        foreach (var entry in Entries)
            entry.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Progress));
    }

    public HabitDefinition Model => _model;
    public int Order => _model.Order;

    [ObservableProperty]
    private string _name;

    public ObservableCollection<HabitEntryViewModel> Entries { get; }

    public int Progress => Entries.Count(e => e.IsCompleted);
    public int Total => Entries.Count;

    partial void OnNameChanged(string value)
    {
        _model.Name = value;
        DebounceService.Debounce($"habit-{_model.Id}",
            () => _service.SaveHabitDefinitionAsync(_model));
    }

    private IEnumerable<HabitEntry> GetOrderedEntries()
    {
        var order = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

        return order.Select(d => _model.Entries.FirstOrDefault(e => e.DayOfWeek == d)
            ?? new HabitEntry { DayOfWeek = d, HabitDefinitionId = _model.Id });
    }
}

public sealed partial class HabitEntryViewModel : ObservableObject
{
    private readonly HabitEntry _model;
    private readonly PlannerService _service;

    public HabitEntryViewModel(HabitEntry model, PlannerService service)
    {
        _model = model;
        _service = service;
        _isCompleted = model.IsCompleted;
    }

    public DayOfWeek DayOfWeek => _model.DayOfWeek;

    [ObservableProperty]
    private bool _isCompleted;

    partial void OnIsCompletedChanged(bool value)
    {
        _model.IsCompleted = value;
        _service.SaveHabitEntryAsync(_model).FireAndForget("habit-entry-save");
    }
}
