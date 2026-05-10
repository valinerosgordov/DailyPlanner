using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class FinanceEntryConfiguration : IEntityTypeConfiguration<FinanceEntry>
{
    public void Configure(EntityTypeBuilder<FinanceEntry> e)
    {
        e.HasKey(fe => fe.Id);
        e.HasIndex(fe => fe.Date);
        e.HasIndex(fe => fe.CategoryId);
        e.HasIndex(fe => fe.RecurringPaymentId);
        e.HasIndex(fe => fe.AccountId);
        e.HasIndex(fe => fe.ParentEntryId);
        e.HasIndex(fe => fe.IsPaid);

        e.Property(fe => fe.Amount).HasColumnType("decimal(18,2)");
        e.Property(fe => fe.Description).HasMaxLength(500);
        e.Property(fe => fe.Currency).HasMaxLength(8).HasDefaultValue("RUB");
        e.Property(fe => fe.ExchangeRateToBase).HasColumnType("decimal(18,6)");
        e.Property(fe => fe.AmountInBaseCurrency).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

        e.HasOne(fe => fe.Week).WithMany().HasForeignKey(fe => fe.WeekId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne(fe => fe.RecurringPayment).WithMany(rp => rp.GeneratedEntries).HasForeignKey(fe => fe.RecurringPaymentId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne(fe => fe.Account).WithMany(a => a.Entries).HasForeignKey(fe => fe.AccountId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne(fe => fe.ParentEntry).WithMany(fe => fe.SplitEntries).HasForeignKey(fe => fe.ParentEntryId).OnDelete(DeleteBehavior.Cascade);
    }
}