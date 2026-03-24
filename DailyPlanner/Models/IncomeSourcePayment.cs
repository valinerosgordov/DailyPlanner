namespace DailyPlanner.Models;

public sealed class IncomeSourcePayment
{
    public int Id { get; set; }
    public int IncomeSourceId { get; set; }
    public int DayOfMonth { get; set; } = 1;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    public IncomeSource? IncomeSource { get; set; }
}
