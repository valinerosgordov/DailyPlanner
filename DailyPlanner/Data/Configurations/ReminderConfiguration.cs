using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> e)
    {
        e.HasKey(r => r.Id);
        e.Property(r => r.Title).HasMaxLength(200);
        e.Property(r => r.Message).HasMaxLength(500);
    }
}
