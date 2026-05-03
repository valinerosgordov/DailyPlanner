using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class FinancialGoalConfiguration : IEntityTypeConfiguration<FinancialGoal>
{
    public void Configure(EntityTypeBuilder<FinancialGoal> e)
    {
        e.HasKey(g => g.Id);
        e.Property(g => g.Name).HasMaxLength(200);
        e.Property(g => g.Icon).HasMaxLength(50);
        e.Property(g => g.Color).HasMaxLength(20);
        e.Property(g => g.TargetAmount).HasColumnType("decimal(18,2)");
        e.Property(g => g.SavedAmount).HasColumnType("decimal(18,2)");
    }
}
