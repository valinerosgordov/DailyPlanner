namespace DailyPlanner.Models;

public sealed class TrelloSettings
{
    public int Id { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ListName { get; set; } = "В работе";
    public bool IsEnabled { get; set; }
    public bool AutoSyncOnStartup { get; set; }

    /// <summary>Two-way sync: archive the Trello card when its task is completed. Opt-in.</summary>
    public bool PushCompletions { get; set; }

    public DateTime? LastSyncUtc { get; set; }
}
