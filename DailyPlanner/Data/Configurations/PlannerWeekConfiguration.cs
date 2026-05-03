using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class PlannerWeekConfiguration : IEntityTypeConfiguration<PlannerWeek>
{
    public void Configure(EntityTypeBuilder<PlannerWeek> e)
    {
        e.HasKey(w => w.Id);
        e.HasIndex(w => w.StartDate).IsUnique();
        e.Property(w => w.Notes).HasMaxLength(4000);
        e.HasMany(w => w.Goals).WithOne(g => g.Week).HasForeignKey(g => g.WeekId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(w => w.Days).WithOne(d => d.Week).HasForeignKey(d => d.WeekId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(w => w.Habits).WithOne(h => h.Week).HasForeignKey(h => h.WeekId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(w => w.WeeklyNotes).WithOne(n => n.Week).HasForeignKey(n => n.WeekId).OnDelete(DeleteBehavior.Cascade);
    }
}
