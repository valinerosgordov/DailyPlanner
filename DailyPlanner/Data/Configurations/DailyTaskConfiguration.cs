using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class DailyTaskConfiguration : IEntityTypeConfiguration<DailyTask>
{
    public void Configure(EntityTypeBuilder<DailyTask> e)
    {
        e.HasKey(t => t.Id);
        e.HasIndex(t => t.DailyPlanId);
        e.HasIndex(t => t.ParentTaskId);
        e.HasIndex(t => t.ExternalId);
        e.Property(t => t.Text).HasMaxLength(500);
        e.Property(t => t.ExternalId).HasMaxLength(100);
        e.HasOne(t => t.ParentTask)
            .WithMany(t => t.SubTasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
