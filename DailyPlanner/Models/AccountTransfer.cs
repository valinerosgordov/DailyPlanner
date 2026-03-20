namespace DailyPlanner.Models;

public sealed class AccountTransfer
{
    public int Id { get; set; }
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Note { get; set; } = string.Empty;

    public Account? FromAccount { get; set; }
    public Account? ToAccount { get; set; }
}
