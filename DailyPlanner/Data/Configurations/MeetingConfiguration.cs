using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> e)
    {
        e.HasKey(m => m.Id);
        e.HasIndex(m => m.DateTime);
        e.Property(m => m.Title).HasMaxLength(300);
        e.Property(m => m.Description).HasMaxLength(2000);
        e.Property(m => m.Attendees).HasMaxLength(1000);
    }
}
