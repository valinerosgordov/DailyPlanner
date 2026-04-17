using DailyPlanner.Data;
using DailyPlanner.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Tests;

/// <summary>
/// Base class: spins up an in-memory SQLite DB and overrides PlannerDbContextFactory
/// so PlannerService methods read/write the test DB instead of the user's real file.
/// </summary>
public abstract class PlannerServiceTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly PlannerService Service;

    protected PlannerServiceTestFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseSqlite(_connection)
            .Options;

        PlannerDbContextFactory.OverrideFactory = () => new PlannerDbContext(options);

        using var ctx = PlannerDbContextFactory.Create();
        ctx.Database.EnsureCreated();

        Service = new PlannerService();
    }

    public void Dispose()
    {
        PlannerDbContextFactory.OverrideFactory = null;
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
