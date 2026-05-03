using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class DailyStateConfiguration : IEntityTypeConfiguration<DailyState>
{
    public void Configure(EntityTypeBuilder<DailyState> e)
    {
        e.HasKey(s => s.Id);
        e.HasIndex(s => s.DailyPlanId).IsUnique();
    }
}
