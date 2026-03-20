namespace DailyPlanner.Models;

public sealed class Debt
{
    public int Id { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public DebtDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly CreatedDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsSettled { get; set; }
    public DateOnly? SettledDate { get; set; }

    public List<DebtPayment> Payments { get; set; } = [];
}
