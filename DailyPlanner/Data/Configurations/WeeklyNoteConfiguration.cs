using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class WeeklyNoteConfiguration : IEntityTypeConfiguration<WeeklyNote>
{
    public void Configure(EntityTypeBuilder<WeeklyNote> e)
    {
        e.HasKey(n => n.Id);
        e.HasIndex(n => n.WeekId);
        e.Property(n => n.Text).HasMaxLength(2000);
    }
}
