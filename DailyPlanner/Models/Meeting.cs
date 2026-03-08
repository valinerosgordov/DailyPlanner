namespace DailyPlanner.Models;

public sealed class Meeting
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Attendees { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public bool NotifyDayBefore { get; set; } = true;
    public bool NotifyTwoHoursBefore { get; set; } = true;
}
