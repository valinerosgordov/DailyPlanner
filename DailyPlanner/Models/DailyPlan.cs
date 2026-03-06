namespace DailyPlanner.Models;

public sealed class DailyPlan
{
    public int Id { get; set; }
    public int WeekId { get; set; }
    public DateOnly Date { get; set; }
    public DayOfWeek DayOfWeek => Date.DayOfWeek;

    public List<DailyTask> Tasks { get; set; } = [];
    public DailyState? State { get; set; }

    public PlannerWeek? Week { get; set; }
}
