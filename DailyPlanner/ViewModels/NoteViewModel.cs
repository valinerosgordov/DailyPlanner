using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class NoteViewModel : ObservableObject
{
    private readonly WeeklyNote _model;
    private readonly PlannerService _service;

    public NoteViewModel(WeeklyNote model, PlannerService service)
    {
        _model = model;
        _service = service;
        _text = model.Text;
    }

    public WeeklyNote Model => _model;
    public int Order => _model.Order;

    [ObservableProperty]
    private string _text;

    partial void OnTextChanged(string value)
    {
        _model.Text = value;
        DebounceService.Debounce($"note-{_model.Id}",
            () => _service.SaveWeeklyNoteAsync(_model));
    }
}
