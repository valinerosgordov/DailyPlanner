namespace DailyPlanner.Models;

public sealed class FinanceEntry
{
    public int Id { get; set; }
    public int? WeekId { get; set; }
    public DateOnly Date { get; set; }
    public int CategoryId { get; set; }
    public FinanceEntryType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public int? RecurringPaymentId { get; set; }
    public bool IsPaid { get; set; } = true;
    public int? AccountId { get; set; }
    public int? ParentEntryId { get; set; }

    public PlannerWeek? Week { get; set; }
    public FinanceCategory? Category { get; set; }
    public RecurringPayment? RecurringPayment { get; set; }
    public Account? Account { get; set; }
    public FinanceEntry? ParentEntry { get; set; }
    public List<FinanceEntry> SplitEntries { get; set; } = [];
}
