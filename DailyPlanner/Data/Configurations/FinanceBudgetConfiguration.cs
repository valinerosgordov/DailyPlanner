using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class FinanceBudgetConfiguration : IEntityTypeConfiguration<FinanceBudget>
{
    public void Configure(EntityTypeBuilder<FinanceBudget> e)
    {
        e.HasKey(b => b.Id);
        e.HasIndex(b => new { b.CategoryId, b.MonthYear }).IsUnique();
        e.Property(b => b.Amount).HasColumnType("decimal(18,2)");
        e.Property(b => b.MonthYear).HasMaxLength(7);
    }
}
