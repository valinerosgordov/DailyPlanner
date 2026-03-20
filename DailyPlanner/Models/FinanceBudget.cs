namespace DailyPlanner.Models;

public sealed class FinanceBudget
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public FinanceCategory? Category { get; set; }
}
