namespace DailyPlanner.Models;

public sealed class TrelloSettings
{
    public int Id { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ListName { get; set; } = "В работе";
    public bool IsEnabled { get; set; }
    public DateTime? LastSyncUtc { get; set; }
}
