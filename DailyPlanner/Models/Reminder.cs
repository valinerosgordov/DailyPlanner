namespace DailyPlanner.Models;

public sealed class Reminder
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public TimeOnly Time { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DayOfWeek? DayOfWeek { get; set; } // null = every day
}
