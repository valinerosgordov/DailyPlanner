using DailyPlanner.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DailyPlanner.Services;

/// <summary>
/// Static DI container. Configure() must be called once at startup before Get&lt;T&gt;() is used.
/// Keeps the registration table in one place; avoids passing IServiceProvider everywhere.
/// </summary>
public static class ServiceHost
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Services => _provider
        ?? throw new InvalidOperationException("ServiceHost.Configure() has not been called.");

    public static void Configure()
    {
        var sc = new ServiceCollection();

        // Core services — singletons (stateless or process-wide)
        sc.AddSingleton<PlannerService>();
        sc.AddSingleton<TrelloService>();
        sc.AddSingleton(_ => new UpdateService("https://github.com/valinerosgordov/DailyPlanner"));

        // Main VM is one per window
        sc.AddSingleton<MainViewModel>();

        _provider = sc.BuildServiceProvider();
    }

    public static T Get<T>() where T : notnull => Services.GetRequiredService<T>();
}
