namespace DailyPlanner.Models;

public sealed class FinancialGoal
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "🎯";
    public string Color { get; set; } = "#cba6f7";
    public decimal TargetAmount { get; set; }
    public decimal SavedAmount { get; set; }
    public DateOnly? TargetDate { get; set; }
    public DateOnly CreatedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public bool IsCompleted { get; set; }
    public int Order { get; set; }
}
