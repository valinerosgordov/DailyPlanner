namespace DailyPlanner.Models;

public sealed class HabitDefinition
{
    public int Id { get; set; }
    public int WeekId { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<HabitEntry> Entries { get; set; } = [];
    public PlannerWeek? Week { get; set; }
}
