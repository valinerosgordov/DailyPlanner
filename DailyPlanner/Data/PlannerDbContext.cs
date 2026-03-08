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
    }
}
