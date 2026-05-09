using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Data;

/// <summary>
/// Adapter: wraps the static <see cref="PlannerDbContextFactory"/> behind the
/// <see cref="IDbContextFactory{TContext}"/> interface so it can be passed to
/// classes that expect DI but are constructed manually (XAML design-time
/// fallback constructors, primarily <see cref="ViewModels.MainViewModel"/>'s
/// parameterless ctor).
/// </summary>
public sealed class PlannerDbContextFactoryAdapter : IDbContextFactory<PlannerDbContext>
{
    public PlannerDbContext CreateDbContext()
    {
        var df = new PlannerDbContextFactory();
        return df.CreateDbContext([]);
    }
}