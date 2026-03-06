namespace DailyPlanner.Models;

public enum TaskPriority { None, Low, Medium, High }
public enum TaskCategory { None, Work, Study, Personal, Health, Other }

public sealed class DailyTask
{
    public int Id { get; set; }
    public int DailyPlanId { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskCategory Category { get; set; }
    public TimeOnly? ReminderTime { get; set; }

    public DailyPlan? DailyPlan { get; set; }
}
