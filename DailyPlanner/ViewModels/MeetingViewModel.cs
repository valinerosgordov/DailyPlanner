using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class MeetingViewModel : ObservableObject
{
    private readonly Meeting _model;
    private readonly PlannerService _service;

    public static int[] Hours { get; } = Enumerable.Range(0, 24).ToArray();
    public static int[] Minutes { get; } = [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55];
    public static int[] Durations { get; } = [15, 30, 45, 60, 90, 120, 180];

    public MeetingViewModel(Meeting model, PlannerService service)
    {
        _model = model;
        _service = service;
        _title = model.Title;
        _description = model.Description;
        _attendees = model.Attendees;
        _meetingDate = model.DateTime.Date;
        _selectedHour = model.DateTime.Hour;
        _selectedMinute = RoundToNearest5(model.DateTime.Minute);
        _durationMinutes = model.DurationMinutes;
        _notifyDayBefore = model.NotifyDayBefore;
        _notifyTwoHoursBefore = model.NotifyTwoHoursBefore;
        _notify30MinBefore = model.Notify30MinBefore;

        // Force ComboBox sync after DataTemplate initialization
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(() =>
        {
            OnPropertyChanged(nameof(SelectedHour));
            OnPropertyChanged(nameof(SelectedMinute));
            OnPropertyChanged(nameof(DurationMinutes));
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    public Meeting Model => _model;

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _description;
    [ObservableProperty] private string _attendees;
    [ObservableProperty] private DateTime _meetingDate;
    [ObservableProperty] private int _selectedHour;
    [ObservableProperty] private int _selectedMinute;
    [ObservableProperty] private int _durationMinutes;
    [ObservableProperty] private bool _notifyDayBefore;
    [ObservableProperty] private bool _notifyTwoHoursBefore;
    [ObservableProperty] private bool _notify30MinBefore;

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

    partial void OnMeetingDateChanged(DateTime value) => UpdateDateTime();
    partial void OnSelectedHourChanged(int value) => UpdateDateTime();
    partial void OnSelectedMinuteChanged(int value) => UpdateDateTime();

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

    partial void OnNotify30MinBeforeChanged(bool value)
    {
        _model.Notify30MinBefore = value;
        Save();
    }

    private void UpdateDateTime()
    {
        _model.DateTime = MeetingDate.Date.AddHours(SelectedHour).AddMinutes(SelectedMinute);
        OnPropertyChanged(nameof(DisplayDate));
        OnPropertyChanged(nameof(DisplayTime));
        OnPropertyChanged(nameof(IsUpcoming));
        OnPropertyChanged(nameof(IsPast));
        Save();
    }

    private void Save()
    {
        DebounceService.Debounce($"meeting-{_model.Id}",
            () => _service.SaveMeetingAsync(_model));
    }

    private static int RoundToNearest5(int minute) =>
        Minutes.MinBy(m => Math.Abs(m - minute));
}
