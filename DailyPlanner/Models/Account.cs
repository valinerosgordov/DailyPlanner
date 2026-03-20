namespace DailyPlanner.Models;

public sealed class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "💳";
    public string Color { get; set; } = "#89b4fa";
    public decimal InitialBalance { get; set; }
    public int Order { get; set; }
    public bool IsArchived { get; set; }

    public List<FinanceEntry> Entries { get; set; } = [];
    public List<AccountTransfer> TransfersFrom { get; set; } = [];
    public List<AccountTransfer> TransfersTo { get; set; } = [];
}
