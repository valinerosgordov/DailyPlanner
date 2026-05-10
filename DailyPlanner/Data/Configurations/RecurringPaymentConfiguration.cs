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
        e.HasIndex(rp => rp.IsSubscription);
        e.HasIndex(rp => rp.NextRenewalDate);

        e.Property(rp => rp.Name).HasMaxLength(200);
        e.Property(rp => rp.Amount).HasColumnType("decimal(18,2)");
        e.Property(rp => rp.Note).HasMaxLength(1000);

        // Subscription fields — explicit defaults so the migration can apply
        // them to existing rows without manual SQL.
        e.Property(rp => rp.IsSubscription).HasDefaultValue(false);
        e.Property(rp => rp.BillingIntervalMonths).HasDefaultValue(1);
        e.Property(rp => rp.ListPriceMonthly).HasColumnType("decimal(18,2)");
        e.Property(rp => rp.AutoRenew).HasDefaultValue(true);
        e.Property(rp => rp.CancellationNoticeDays).HasDefaultValue(0);
        e.Property(rp => rp.RenewalRemindDaysBefore).HasMaxLength(64).HasDefaultValue(string.Empty);
        e.Property(rp => rp.Currency).HasMaxLength(8).HasDefaultValue("RUB");
    }
}