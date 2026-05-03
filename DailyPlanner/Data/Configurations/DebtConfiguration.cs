using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> e)
    {
        e.HasKey(d => d.Id);
        e.Property(d => d.PersonName).HasMaxLength(200);
        e.Property(d => d.Description).HasMaxLength(500);
        e.Property(d => d.Amount).HasColumnType("decimal(18,2)");
        e.HasMany(d => d.Payments).WithOne(p => p.Debt).HasForeignKey(p => p.DebtId).OnDelete(DeleteBehavior.Cascade);
    }
}
