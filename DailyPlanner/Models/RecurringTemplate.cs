namespace DailyPlanner.Models;

public sealed class RecurringTemplate
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public TaskCategory Category { get; set; }
    public DayOfWeek? DayOfWeek { get; set; } // null = every day
    public bool IsActive { get; set; } = true;
}
