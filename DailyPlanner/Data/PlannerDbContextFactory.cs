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
        var factory = new PlannerDbContextFactory();
        return factory.CreateDbContext([]);
    }
}
