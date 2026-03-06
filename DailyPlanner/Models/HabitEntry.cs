namespace DailyPlanner.Models;

public sealed class HabitEntry
{
    public int Id { get; set; }
    public int HabitDefinitionId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsCompleted { get; set; }

    public HabitDefinition? HabitDefinition { get; set; }
}
