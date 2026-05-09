using System.Data.Common;
using DailyPlanner.Data;
using DailyPlanner.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Tests;

/// <summary>
/// Base class: spins up an in-memory SQLite DB and feeds a corresponding
/// IDbContextFactory directly into PlannerService. Replaces the previous
/// PlannerDbContextFactory.OverrideFactory mutable-static approach, which
/// could race-contaminate across xUnit's parallel test classes.
///
/// Each fixture instance owns its own SqliteConnection (kept open for the
/// lifetime of the fixture so the in-memory DB survives between calls) and
/// a local IDbContextFactory adapter that hands out PlannerDbContexts bound
/// to that connection.
/// </summary>
public abstract class PlannerServiceTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly PlannerService Service;

    protected PlannerServiceTestFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var dbFactory = new InMemorySqliteDbFactory(_connection);

        // Run real migrations instead of EnsureCreated so tests catch
        // schema drift between Configurations and migration files.
        using (var ctx = dbFactory.CreateDbContext())
        {
            ctx.Database.Migrate();
        }

        Service = new PlannerService(dbFactory);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Test-local IDbContextFactory: every CreateDbContext() shares the same
    /// SqliteConnection so all contexts see the same in-memory DB. This is
    /// the standard pattern for in-memory SQLite tests.
    /// </summary>
    private sealed class InMemorySqliteDbFactory : IDbContextFactory<PlannerDbContext>
    {
        private readonly DbConnection _connection;

        public InMemorySqliteDbFactory(DbConnection connection)
        {
            _connection = connection;
        }

        public PlannerDbContext CreateDbContext()
        {
            var opts = new DbContextOptionsBuilder<PlannerDbContext>()
                .UseSqlite(_connection)
                .Options;
            return new PlannerDbContext(opts);
        }
    }
}