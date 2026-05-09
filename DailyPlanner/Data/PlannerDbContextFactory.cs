using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DailyPlanner.Data;

/// <summary>
/// Design-time DbContext factory for EF Core CLI (migrations).
///
/// Runtime callers should consume <see cref="IDbContextFactory{PlannerDbContext}"/>
/// from the DI container instead — see ServiceHost.Configure. Keeping this
/// class around is necessary because <c>dotnet ef</c> instantiates it
/// reflectively, but it should not be invoked from production code paths.
///
/// AppDataFolder / DbPath remain static path utilities (used by App.xaml.cs
/// startup, backup, and recovery flows that legitimately run before DI is up).
/// </summary>
public sealed class PlannerDbContextFactory : IDesignTimeDbContextFactory<PlannerDbContext>
{
    public static string AppDataFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DailyPlanner");

    public static string DbPath => Path.Combine(AppDataFolder, "planner.db");

    public PlannerDbContext CreateDbContext(string[] args)
    {
        var dir = Path.GetDirectoryName(DbPath)!;
        Directory.CreateDirectory(dir);

        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseSqlite($"Data Source={DbPath}")
            .Options;

        return new PlannerDbContext(options);
    }

    /// <summary>
    /// Bootstrap helper used only by App.xaml.cs migration step (runs before
    /// the DI container exists). Production services must NOT use this —
    /// inject <see cref="IDbContextFactory{PlannerDbContext}"/> instead.
    /// </summary>
    public static PlannerDbContext Create()
    {
        var factory = new PlannerDbContextFactory();
        return factory.CreateDbContext([]);
    }
}