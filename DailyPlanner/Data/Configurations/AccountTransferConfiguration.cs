using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyPlanner.Data.Configurations;

public sealed class AccountTransferConfiguration : IEntityTypeConfiguration<AccountTransfer>
{
    public void Configure(EntityTypeBuilder<AccountTransfer> e)
    {
        e.HasKey(t => t.Id);
        e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        e.Property(t => t.Note).HasMaxLength(500);
        e.HasOne(t => t.FromAccount).WithMany(a => a.TransfersFrom).HasForeignKey(t => t.FromAccountId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(t => t.ToAccount).WithMany(a => a.TransfersTo).HasForeignKey(t => t.ToAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
