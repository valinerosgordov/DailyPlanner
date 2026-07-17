using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class DebtPaymentConfiguration : IEntityTypeConfiguration<DebtPayment>
{
    public void Configure(EntityTypeBuilder<DebtPayment> e)
    {
        e.HasKey(p => p.Id);
        e.HasIndex(p => p.DebtId);
        e.Property(p => p.Note).HasMaxLength(500);
    }
}
