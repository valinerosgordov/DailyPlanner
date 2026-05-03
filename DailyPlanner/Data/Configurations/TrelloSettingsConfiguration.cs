using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class TrelloSettingsConfiguration : IEntityTypeConfiguration<TrelloSettings>
{
    public void Configure(EntityTypeBuilder<TrelloSettings> e)
    {
        e.HasKey(s => s.Id);
        e.Property(s => s.ApiKey).HasMaxLength(200);
        e.Property(s => s.Token).HasMaxLength(200);
        e.Property(s => s.ListName).HasMaxLength(200);
    }
}
