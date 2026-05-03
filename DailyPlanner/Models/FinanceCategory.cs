namespace DailyPlanner.Models;

public sealed class FinanceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public FinanceEntryType Type { get; set; }
    public int Order { get; set; }
    public bool IsArchived { get; set; }
    // Stable seed identifier (e.g. "Salary", "Food"). Used to rename Name on
    // language switch. NULL for user-created categories.
    public string? SeedKey { get; set; }

    public List<FinanceEntry> Entries { get; set; } = [];
    public List<FinanceBudget> Budgets { get; set; } = [];
    public List<RecurringPayment> RecurringPayments { get; set; } = [];
}
