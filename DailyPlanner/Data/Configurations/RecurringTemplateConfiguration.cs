using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class RecurringTemplateConfiguration : IEntityTypeConfiguration<RecurringTemplate>
{
    public void Configure(EntityTypeBuilder<RecurringTemplate> e)
    {
        e.HasKey(rt => rt.Id);
        e.Property(rt => rt.Text).HasMaxLength(500);
    }
}
