namespace DailyPlanner.Models;

public sealed class PlannerWeek
{
    public int Id { get; set; }
    public DateOnly StartDate { get; set; }
    public string Notes { get; set; } = string.Empty;

    public List<WeeklyGoal> Goals { get; set; } = [];
    public List<DailyPlan> Days { get; set; } = [];
    public List<HabitDefinition> Habits { get; set; } = [];
    public List<WeeklyNote> WeeklyNotes { get; set; } = [];
}
