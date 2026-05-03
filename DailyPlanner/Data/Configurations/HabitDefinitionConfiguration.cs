using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class HabitDefinitionConfiguration : IEntityTypeConfiguration<HabitDefinition>
{
    public void Configure(EntityTypeBuilder<HabitDefinition> e)
    {
        e.HasKey(h => h.Id);
        e.HasIndex(h => h.WeekId);
        e.Property(h => h.Name).HasMaxLength(200);
        e.HasMany(h => h.Entries).WithOne(he => he.HabitDefinition).HasForeignKey(he => he.HabitDefinitionId).OnDelete(DeleteBehavior.Cascade);
    }
}
