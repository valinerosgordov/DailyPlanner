using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class RecurringPaymentConfiguration : IEntityTypeConfiguration<RecurringPayment>
{
    public void Configure(EntityTypeBuilder<RecurringPayment> e)
    {
        e.HasKey(rp => rp.Id);
        e.HasIndex(rp => rp.CategoryId);
        e.HasIndex(rp => rp.IsActive);
        e.Property(rp => rp.Name).HasMaxLength(200);
        e.Property(rp => rp.Amount).HasColumnType("decimal(18,2)");
        e.Property(rp => rp.Note).HasMaxLength(1000);
    }
}
