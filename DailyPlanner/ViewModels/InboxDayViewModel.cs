using CommunityToolkit.Mvvm.ComponentModel;

namespace DailyPlanner.ViewModels;

public sealed partial class InboxDayViewModel : ObservableObject
{
    public DateOnly Date { get; }
    public string DayName { get; }
    public string DateLabel { get; }
    public bool IsToday { get; }

    [ObservableProperty] private bool _isDropTarget;

    public InboxDayViewModel(DateOnly date)
    {
        Date = date;
        DayName = Services.Loc.GetDayName(date.DayOfWeek);
        DateLabel = date.ToString("dd.MM");
        IsToday = date == DateOnly.FromDateTime(DateTime.Today);
    }
}
