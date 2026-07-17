using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class HabitEntryConfiguration : IEntityTypeConfiguration<HabitEntry>
{
    public void Configure(EntityTypeBuilder<HabitEntry> e)
    {
        e.HasKey(he => he.Id);
        // Domain invariant: exactly one entry per habit per weekday. Creation is
        // funneled through GetOrCreateWeekAsync, but only the schema can guarantee it.
        e.HasIndex(he => new { he.HabitDefinitionId, he.DayOfWeek }).IsUnique();
    }
}
