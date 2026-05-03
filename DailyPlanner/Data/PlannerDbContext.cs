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
    public DbSet<InboxTask> InboxTasks => Set<InboxTask>();
    public DbSet<TrelloSettings> TrelloSettings => Set<TrelloSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlannerDbContext).Assembly);
    }
}
