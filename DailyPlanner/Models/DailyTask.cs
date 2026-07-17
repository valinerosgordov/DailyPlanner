namespace DailyPlanner.Models;

public enum TaskPriority { None, Low, Medium, High }
public enum TaskCategory { None, Work, Study, Personal, Health, Other }

public sealed class DailyTask
{
    public int Id { get; set; }
    public int DailyPlanId { get; set; }
    public int? ParentTaskId { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskCategory Category { get; set; }
    public TimeOnly? ReminderTime { get; set; }
    public DateOnly? Deadline { get; set; }
    public string? ExternalId { get; set; }

    /// <summary>
    /// When the completion of this task was pushed to Trello (card archived).
    /// Null = not pushed yet (retried on the next push pass) or not a Trello task.
    /// Cleared when the task is un-completed and the card gets un-archived.
    /// </summary>
    public DateTime? ExternalClosedUtc { get; set; }

    public DailyPlan? DailyPlan { get; set; }
    public DailyTask? ParentTask { get; set; }
    public List<DailyTask> SubTasks { get; set; } = [];
}
