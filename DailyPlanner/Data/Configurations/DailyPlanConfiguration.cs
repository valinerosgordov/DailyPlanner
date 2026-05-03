using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class DailyPlanConfiguration : IEntityTypeConfiguration<DailyPlan>
{
    public void Configure(EntityTypeBuilder<DailyPlan> e)
    {
        e.HasKey(d => d.Id);
        e.HasIndex(d => new { d.WeekId, d.Date }).IsUnique();
        e.HasMany(d => d.Tasks).WithOne(t => t.DailyPlan).HasForeignKey(t => t.DailyPlanId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(d => d.State).WithOne(s => s.DailyPlan).HasForeignKey<DailyState>(s => s.DailyPlanId).OnDelete(DeleteBehavior.Cascade);
        e.Ignore(d => d.DayOfWeek);
    }
}
