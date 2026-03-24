namespace DailyPlanner.Models;

public sealed class IncomeSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Icon { get; set; } = "\U0001F4BC"; // briefcase
    public string Color { get; set; } = "#6C63FF";
    public decimal TotalMonthlyAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public string Note { get; set; } = string.Empty;
    public int Order { get; set; }

    public List<IncomeSourcePayment> Payments { get; set; } = [];
}
