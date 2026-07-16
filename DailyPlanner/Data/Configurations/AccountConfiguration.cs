using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> e)
    {
        e.HasKey(a => a.Id);
        e.Property(a => a.Name).HasMaxLength(200);
        e.Property(a => a.Icon).HasMaxLength(50);
        e.Property(a => a.Color).HasMaxLength(20);
    }
}
