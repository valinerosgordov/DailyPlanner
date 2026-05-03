using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class FinanceCategoryConfiguration : IEntityTypeConfiguration<FinanceCategory>
{
    public void Configure(EntityTypeBuilder<FinanceCategory> e)
    {
        e.HasKey(c => c.Id);
        e.Property(c => c.Name).HasMaxLength(200);
        e.Property(c => c.Icon).HasMaxLength(50);
        e.Property(c => c.Color).HasMaxLength(20);
        e.Property(c => c.SeedKey).HasMaxLength(50);
        e.HasIndex(c => c.SeedKey);
        e.HasMany(c => c.Entries).WithOne(fe => fe.Category).HasForeignKey(fe => fe.CategoryId).OnDelete(DeleteBehavior.Restrict);
        e.HasMany(c => c.Budgets).WithOne(b => b.Category).HasForeignKey(b => b.CategoryId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(c => c.RecurringPayments).WithOne(rp => rp.Category).HasForeignKey(rp => rp.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
