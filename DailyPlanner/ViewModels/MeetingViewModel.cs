using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class MeetingViewModel : ObservableObject
{
    private readonly Meeting _model;
    private readonly PlannerService _service;

    public MeetingViewModel(Meeting model, PlannerService service)
    {
        _model = model;
        _service = service;
        _title = model.Title;
        _description = model.Description;
        _attendees = model.Attendees;
        _meetingDate = model.DateTime.Date;
        _timeText = model.DateTime.ToString("HH:mm");
        _durationMinutes = model.DurationMinutes;
        _notifyDayBefore = model.NotifyDayBefore;
        _notifyTwoHoursBefore = model.NotifyTwoHoursBefore;
    }

    public Meeting Model => _model;

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _description;
    [ObservableProperty] private string _attendees;
    [ObservableProperty] private DateTime _meetingDate;
    [ObservableProperty] private string _timeText;
    [ObservableProperty] private int _durationMinutes;
    [ObservableProperty] private bool _notifyDayBefore;
    [ObservableProperty] private bool _notifyTwoHoursBefore;

    public string DisplayDate => _model.DateTime.ToString("dd.MM.yyyy");
    public string DisplayTime => _model.DateTime.ToString("HH:mm");
    public string DisplayDuration => $"{DurationMinutes} {Loc.Get("MeetingMin")}";

    public bool IsUpcoming => _model.DateTime > DateTime.Now;
    public bool IsPast => _model.DateTime.AddMinutes(DurationMinutes) < DateTime.Now;

    partial void OnTitleChanged(string value)
    {
        _model.Title = value;
        Save();
    }

    partial void OnDescriptionChanged(string value)
    {
        _model.Description = value;
        Save();
    }

    partial void OnAttendeesChanged(string value)
    {
        _model.Attendees = value;
        Save();
    }

    partial void OnMeetingDateChanged(DateTime value)
    {
        UpdateDateTime();
    }

    partial void OnTimeTextChanged(string value)
    {
        UpdateDateTime();
    }

    partial void OnDurationMinutesChanged(int value)
    {
        _model.DurationMinutes = value;
        OnPropertyChanged(nameof(DisplayDuration));
        Save();
    }

    partial void OnNotifyDayBeforeChanged(bool value)
    {
        _model.NotifyDayBefore = value;
        Save();
    }

    partial void OnNotifyTwoHoursBeforeChanged(bool value)
    {
        _model.NotifyTwoHoursBefore = value;
        Save();
    }

    private void UpdateDateTime()
    {
        if (TimeOnly.TryParse(TimeText, out var time))
        {
            _model.DateTime = MeetingDate.Date.Add(time.ToTimeSpan());
            OnPropertyChanged(nameof(DisplayDate));
            OnPropertyChanged(nameof(DisplayTime));
            OnPropertyChanged(nameof(IsUpcoming));
            OnPropertyChanged(nameof(IsPast));
            Save();
        }
    }

    private void Save()
    {
        DebounceService.Debounce($"meeting-{_model.Id}",
            () => _service.SaveMeetingAsync(_model));
    }
}
