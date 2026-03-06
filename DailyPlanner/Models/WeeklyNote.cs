namespace DailyPlanner.Models;

public sealed class WeeklyNote
{
    public int Id { get; set; }
    public int WeekId { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;

    public PlannerWeek? Week { get; set; }
}
