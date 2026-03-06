namespace DailyPlanner.Models;

public sealed class WeeklyGoal
{
    public int Id { get; set; }
    public int WeekId { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }

    public PlannerWeek? Week { get; set; }
}
