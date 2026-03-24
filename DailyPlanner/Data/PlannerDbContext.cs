using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Data;

public sealed class PlannerDbContext(DbContextOptions<PlannerDbContext> options) : DbContext(options)
{
    public DbSet<PlannerWeek> Weeks => Set<PlannerWeek>();
    public DbSet<WeeklyGoal> WeeklyGoals => Set<WeeklyGoal>();
    public DbSet<DailyPlan> DailyPlans => Set<DailyPlan>();
    public DbSet<DailyTask> DailyTasks => Set<DailyTask>();
    public DbSet<DailyState> DailyStates => Set<DailyState>();
    public DbSet<HabitDefinition> HabitDefinitions => Set<HabitDefinition>();
    public DbSet<HabitEntry> HabitEntries => Set<HabitEntry>();
    public DbSet<RecurringTemplate> RecurringTemplates => Set<RecurringTemplate>();
    public DbSet<WeeklyNote> WeeklyNotes => Set<WeeklyNote>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<FinanceCategory> FinanceCategories => Set<FinanceCategory>();
    public DbSet<FinanceEntry> FinanceEntries => Set<FinanceEntry>();
    public DbSet<FinanceBudget> FinanceBudgets => Set<FinanceBudget>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<DebtPayment> DebtPayments => Set<DebtPayment>();
    public DbSet<RecurringPayment> RecurringPayments => Set<RecurringPayment>();
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountTransfer> AccountTransfers => Set<AccountTransfer>();
    public DbSet<IncomeSource> IncomeSources => Set<IncomeSource>();
    public DbSet<IncomeSourcePayment> IncomeSourcePayments => Set<IncomeSourcePayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlannerWeek>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.StartDate).IsUnique();
            e.Property(w => w.Notes).HasMaxLength(4000);
            e.HasMany(w => w.Goals).WithOne(g => g.Week).HasForeignKey(g => g.WeekId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(w => w.Days).WithOne(d => d.Week).HasForeignKey(d => d.WeekId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(w => w.Habits).WithOne(h => h.Week).HasForeignKey(h => h.WeekId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(w => w.WeeklyNotes).WithOne(n => n.Week).HasForeignKey(n => n.WeekId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyPlan>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => new { d.WeekId, d.Date }).IsUnique();
            e.HasMany(d => d.Tasks).WithOne(t => t.DailyPlan).HasForeignKey(t => t.DailyPlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.State).WithOne(s => s.DailyPlan).HasForeignKey<DailyState>(s => s.DailyPlanId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(d => d.DayOfWeek);
        });

        modelBuilder.Entity<WeeklyGoal>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => g.WeekId);
            e.Property(g => g.Text).HasMaxLength(500);
        });

        modelBuilder.Entity<DailyTask>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.DailyPlanId);
            e.HasIndex(t => t.ParentTaskId);
            e.Property(t => t.Text).HasMaxLength(500);
            e.HasOne(t => t.ParentTask)
                .WithMany(t => t.SubTasks)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyState>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.DailyPlanId).IsUnique();
        });

        modelBuilder.Entity<HabitDefinition>(e =>
        {
            e.HasKey(h => h.Id);
            e.HasIndex(h => h.WeekId);
            e.Property(h => h.Name).HasMaxLength(200);
            e.HasMany(h => h.Entries).WithOne(he => he.HabitDefinition).HasForeignKey(he => he.HabitDefinitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HabitEntry>(e =>
        {
            e.HasKey(he => he.Id);
            e.HasIndex(he => he.HabitDefinitionId);
        });

        modelBuilder.Entity<RecurringTemplate>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.Property(rt => rt.Text).HasMaxLength(500);
        });

        modelBuilder.Entity<WeeklyNote>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => n.WeekId);
            e.Property(n => n.Text).HasMaxLength(2000);
        });

        modelBuilder.Entity<Reminder>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Title).HasMaxLength(200);
            e.Property(r => r.Message).HasMaxLength(500);
        });

        modelBuilder.Entity<Meeting>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.DateTime);
            e.Property(m => m.Title).HasMaxLength(300);
            e.Property(m => m.Description).HasMaxLength(2000);
            e.Property(m => m.Attendees).HasMaxLength(1000);
        });

        // ─── Finance ───────────────────────────────────────────────────

        modelBuilder.Entity<FinanceCategory>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Icon).HasMaxLength(50);
            e.Property(c => c.Color).HasMaxLength(20);
            e.HasMany(c => c.Entries).WithOne(fe => fe.Category).HasForeignKey(fe => fe.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Budgets).WithOne(b => b.Category).HasForeignKey(b => b.CategoryId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.RecurringPayments).WithOne(rp => rp.Category).HasForeignKey(rp => rp.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinanceEntry>(e =>
        {
            e.HasKey(fe => fe.Id);
            e.HasIndex(fe => fe.Date);
            e.HasIndex(fe => fe.CategoryId);
            e.HasIndex(fe => fe.RecurringPaymentId);
            e.Property(fe => fe.Amount).HasColumnType("decimal(18,2)");
            e.Property(fe => fe.Description).HasMaxLength(500);
            e.HasOne(fe => fe.Week).WithMany().HasForeignKey(fe => fe.WeekId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(fe => fe.RecurringPayment).WithMany(rp => rp.GeneratedEntries).HasForeignKey(fe => fe.RecurringPaymentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(fe => fe.Account).WithMany(a => a.Entries).HasForeignKey(fe => fe.AccountId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(fe => fe.AccountId);
            e.HasOne(fe => fe.ParentEntry).WithMany(fe => fe.SplitEntries).HasForeignKey(fe => fe.ParentEntryId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(fe => fe.ParentEntryId);
        });

        modelBuilder.Entity<FinanceBudget>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.CategoryId, b.MonthYear }).IsUnique();
            e.Property(b => b.Amount).HasColumnType("decimal(18,2)");
            e.Property(b => b.MonthYear).HasMaxLength(7);
        });

        modelBuilder.Entity<Debt>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.PersonName).HasMaxLength(200);
            e.Property(d => d.Description).HasMaxLength(500);
            e.Property(d => d.Amount).HasColumnType("decimal(18,2)");
            e.HasMany(d => d.Payments).WithOne(p => p.Debt).HasForeignKey(p => p.DebtId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DebtPayment>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.DebtId);
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.Property(p => p.Note).HasMaxLength(500);
        });

        modelBuilder.Entity<RecurringPayment>(e =>
        {
            e.HasKey(rp => rp.Id);
            e.HasIndex(rp => rp.CategoryId);
            e.HasIndex(rp => rp.IsActive);
            e.Property(rp => rp.Name).HasMaxLength(200);
            e.Property(rp => rp.Amount).HasColumnType("decimal(18,2)");
            e.Property(rp => rp.Note).HasMaxLength(1000);
        });

        modelBuilder.Entity<FinancialGoal>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Name).HasMaxLength(200);
            e.Property(g => g.Icon).HasMaxLength(50);
            e.Property(g => g.Color).HasMaxLength(20);
            e.Property(g => g.TargetAmount).HasColumnType("decimal(18,2)");
            e.Property(g => g.SavedAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).HasMaxLength(200);
            e.Property(a => a.Icon).HasMaxLength(50);
            e.Property(a => a.Color).HasMaxLength(20);
            e.Property(a => a.InitialBalance).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<AccountTransfer>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.Note).HasMaxLength(500);
            e.HasOne(t => t.FromAccount).WithMany(a => a.TransfersFrom).HasForeignKey(t => t.FromAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.ToAccount).WithMany(a => a.TransfersTo).HasForeignKey(t => t.ToAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IncomeSource>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(200);
            e.Property(s => s.ClientName).HasMaxLength(200);
            e.Property(s => s.Icon).HasMaxLength(50);
            e.Property(s => s.Color).HasMaxLength(20);
            e.Property(s => s.TotalMonthlyAmount).HasColumnType("decimal(18,2)");
            e.Property(s => s.Note).HasMaxLength(1000);
            e.HasMany(s => s.Payments).WithOne(p => p.IncomeSource).HasForeignKey(p => p.IncomeSourceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IncomeSourcePayment>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.IncomeSourceId);
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.Property(p => p.Description).HasMaxLength(500);
        });
    }
}
