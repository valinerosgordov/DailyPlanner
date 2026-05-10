using DailyPlanner.Data;
using DailyPlanner.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DailyPlanner.Services;

/// <summary>
/// Static facade over the DI container. Configure() must be called once at
/// startup before Get&lt;T&gt;() is used. Keeps the registration table in one
/// place; avoids passing IServiceProvider everywhere.
/// </summary>
public static class ServiceHost
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Services => _provider
        ?? throw new InvalidOperationException("ServiceHost.Configure() has not been called.");

    public static void Configure()
    {
        var sc = new ServiceCollection();

        // EF Core DbContext factory — produces a fresh DbContext per call.
        // PlannerService partials open and dispose contexts in tight scopes,
        // so factory pattern matches the lifetime they expect.
        sc.AddDbContextFactory<PlannerDbContext>(opts =>
            opts.UseSqlite($"Data Source={PlannerDbContextFactory.DbPath}"));

        // Core services — singletons (stateless or process-wide).
        sc.AddSingleton(TimeProvider.System);
        sc.AddSingleton<PlannerService>();
        sc.AddSingleton<TrelloService>();
        sc.AddSingleton<ExchangeRateService>();
        sc.AddSingleton(_ => new UpdateService("https://github.com/valinerosgordov/DailyPlanner"));

        // Main VM is one per window.
        sc.AddSingleton<MainViewModel>();

        _provider = sc.BuildServiceProvider();
    }

    /// <summary>
    /// Replaces the active provider with one built from a custom configuration
    /// callback. Test-only: lets fixtures register their own DbContextFactory
    /// (e.g. backed by an in-memory SQLite connection) without touching the
    /// removed PlannerDbContextFactory.OverrideFactory mutable static.
    /// </summary>
    public static void ConfigureForTests(Action<IServiceCollection> configure)
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(TimeProvider.System);
        sc.AddSingleton<PlannerService>();
        configure(sc);
        _provider = sc.BuildServiceProvider();
    }

    public static T Get<T>() where T : notnull => Services.GetRequiredService<T>();
}