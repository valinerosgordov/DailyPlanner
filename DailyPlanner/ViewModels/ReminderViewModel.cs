using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class ReminderViewModel : ObservableObject
{
    private readonly Reminder _model;
    private readonly PlannerService _service;

    public ReminderViewModel(Reminder model, PlannerService service)
    {
        _model = model;
        _service = service;
        _title = model.Title;
        _message = model.Message;
        _timeText = model.Time.ToString("HH:mm");
        _isEnabled = model.IsEnabled;
    }

    public Reminder Model => _model;

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _message;
    [ObservableProperty] private string _timeText;
    [ObservableProperty] private bool _isEnabled;

    partial void OnTitleChanged(string value)
    {
        _model.Title = value;
        Save();
    }

    partial void OnMessageChanged(string value)
    {
        _model.Message = value;
        Save();
    }

    partial void OnTimeTextChanged(string value)
    {
        if (TimeOnly.TryParse(value, out var time))
        {
            _model.Time = time;
            Save();
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _model.IsEnabled = value;
        DebounceService.Debounce($"reminder-{_model.Id}",
            () => _service.SaveReminderAsync(_model));
    }

    private void Save()
    {
        DebounceService.Debounce($"reminder-{_model.Id}",
            () => _service.SaveReminderAsync(_model));
    }
}
