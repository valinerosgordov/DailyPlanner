namespace DailyPlanner.Models;

public sealed class RecurringPayment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public FinanceEntryType Type { get; set; }
    public int CategoryId { get; set; }
    public PaymentFrequency Frequency { get; set; }
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoCreate { get; set; }
    public int RemindDaysBefore { get; set; }
    public string Note { get; set; } = string.Empty;

    public FinanceCategory? Category { get; set; }
    public List<FinanceEntry> GeneratedEntries { get; set; } = [];
}
