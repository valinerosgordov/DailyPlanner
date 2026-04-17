using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DailyPlanner.Data;

public sealed class PlannerDbContextFactory : IDesignTimeDbContextFactory<PlannerDbContext>
{
    public static string AppDataFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DailyPlanner");

    public static string DbPath => Path.Combine(AppDataFolder, "planner.db");

    /// <summary>
    /// Optional override for tests — when set, Create() will use this factory
    /// instead of opening the real user SQLite file.
    /// </summary>
    public static Func<PlannerDbContext>? OverrideFactory { get; set; }

    public PlannerDbContext CreateDbContext(string[] args)
    {
        var dir = Path.GetDirectoryName(DbPath)!;
        Directory.CreateDirectory(dir);

        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseSqlite($"Data Source={DbPath}")
            .Options;

        return new PlannerDbContext(options);
    }

    public static PlannerDbContext Create()
    {
        if (OverrideFactory is not null) return OverrideFactory();
        var factory = new PlannerDbContextFactory();
        return factory.CreateDbContext([]);
    }
}
