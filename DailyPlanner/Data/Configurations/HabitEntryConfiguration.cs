using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class HabitEntryConfiguration : IEntityTypeConfiguration<HabitEntry>
{
    public void Configure(EntityTypeBuilder<HabitEntry> e)
    {
        e.HasKey(he => he.Id);
        e.HasIndex(he => he.HabitDefinitionId);
    }
}
