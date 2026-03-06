namespace DailyPlanner.Models;

public sealed class DailyState
{
    public int Id { get; set; }
    public int DailyPlanId { get; set; }
    public int Sleep { get; set; }
    public int Energy { get; set; }
    public int Mood { get; set; }

    public DailyPlan? DailyPlan { get; set; }
}
