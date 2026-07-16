using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class InboxTaskConfiguration : IEntityTypeConfiguration<InboxTask>
{
    public void Configure(EntityTypeBuilder<InboxTask> e)
    {
        e.HasKey(t => t.Id);
        // Schema-level Trello dedup: SyncTrelloAsync's app-level HashSet check has
        // a race window; the partial unique index makes a duplicate card impossible.
        e.HasIndex(t => t.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
        e.HasIndex(t => t.IsArchived);
        e.Property(t => t.Text).HasMaxLength(500);
        e.Property(t => t.ExternalId).HasMaxLength(100);
        e.Property(t => t.BoardName).HasMaxLength(200);
        e.Property(t => t.ListName).HasMaxLength(200);
        e.Property(t => t.Url).HasMaxLength(500);
    }
}
