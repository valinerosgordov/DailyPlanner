using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class IncomeSourceConfiguration : IEntityTypeConfiguration<IncomeSource>
{
    public void Configure(EntityTypeBuilder<IncomeSource> e)
    {
        e.HasKey(s => s.Id);
        e.Property(s => s.Name).HasMaxLength(200);
        e.Property(s => s.ClientName).HasMaxLength(200);
        e.Property(s => s.Icon).HasMaxLength(50);
        e.Property(s => s.Color).HasMaxLength(20);
        e.Property(s => s.Note).HasMaxLength(1000);
        e.HasMany(s => s.Payments).WithOne(p => p.IncomeSource).HasForeignKey(p => p.IncomeSourceId).OnDelete(DeleteBehavior.Cascade);
    }
}
