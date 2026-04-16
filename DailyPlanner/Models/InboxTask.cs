namespace DailyPlanner.Models;

public enum InboxSource { Manual, Trello }

public sealed class InboxTask
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public InboxSource Source { get; set; }
    public string? ExternalId { get; set; }
    public string? BoardName { get; set; }
    public string? ListName { get; set; }
    public string? Url { get; set; }
    public DateOnly CreatedDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskCategory Category { get; set; }
    public bool IsArchived { get; set; }
}
