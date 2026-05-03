using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class IncomeSourcePaymentConfiguration : IEntityTypeConfiguration<IncomeSourcePayment>
{
    public void Configure(EntityTypeBuilder<IncomeSourcePayment> e)
    {
        e.HasKey(p => p.Id);
        e.HasIndex(p => p.IncomeSourceId);
        e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        e.Property(p => p.Description).HasMaxLength(500);
    }
}
