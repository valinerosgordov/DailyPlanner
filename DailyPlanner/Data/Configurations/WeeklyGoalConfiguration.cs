using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class WeeklyGoalConfiguration : IEntityTypeConfiguration<WeeklyGoal>
{
    public void Configure(EntityTypeBuilder<WeeklyGoal> e)
    {
        e.HasKey(g => g.Id);
        e.HasIndex(g => g.WeekId);
        e.Property(g => g.Text).HasMaxLength(500);
    }
}
