namespace DailyPlanner.Models;

public sealed class DebtPayment
{
    public int Id { get; set; }
    public int DebtId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string Note { get; set; } = string.Empty;

    public Debt? Debt { get; set; }
}
