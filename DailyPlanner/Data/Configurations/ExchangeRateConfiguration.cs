using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> e)
    {
        e.HasKey(x => x.Id);
        // One rate per (currency, base, date). Re-running the daily fetch
        // updates the same row instead of accumulating duplicates.
        e.HasIndex(x => new { x.CurrencyCode, x.BaseCurrency, x.Date }).IsUnique();
        e.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        e.Property(x => x.BaseCurrency).HasMaxLength(8).IsRequired();
        e.Property(x => x.Rate).HasColumnType("decimal(18,6)");
        e.Property(x => x.Source).HasMaxLength(64);
    }
}