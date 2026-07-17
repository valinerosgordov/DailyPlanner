using System.IO;
using DailyPlanner.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;

namespace DailyPlanner.Tests;

/// <summary>
/// DbIntegrity guards against a REAL past incident: a corrupted live DB was
/// backed up in a loop until every healthy backup in the retention window was
/// overwritten. These tests pin the gate's behavior.
/// </summary>
public sealed class DbIntegrityTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "DailyPlannerTests", Guid.NewGuid().ToString("N"));

    public DbIntegrityTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch (IOException) { /* pooled handles */ }
    }

    private string PathFor(string name) => Path.Combine(_dir, name);

    private string CreateHealthyDb(string name, int rows = 3)
    {
        var path = PathFor(name);
        using var conn = new SqliteConnection($"Data Source={path}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE t(id INTEGER PRIMARY KEY, v TEXT);";
        cmd.ExecuteNonQuery();
        for (var i = 0; i < rows; i++)
        {
            cmd.CommandText = $"INSERT INTO t(v) VALUES ('row{i}');";
            cmd.ExecuteNonQuery();
        }
        SqliteConnection.ClearAllPools();
        return path;
    }

    [Fact]
    public void Check_HealthyDb_ReturnsOk()
    {
        var db = CreateHealthyDb("healthy.db");

        DbIntegrity.Check(db).Should().Be(DbIntegrity.OkResult);
        DbIntegrity.IsHealthy(db).Should().BeTrue();
    }

    [Fact]
    public void Check_MissingFile_ReturnsMissingFile()
    {
        DbIntegrity.Check(PathFor("nope.db")).Should().Be("missing-file");
        DbIntegrity.IsHealthy(PathFor("nope.db")).Should().BeFalse();
    }

    [Fact]
    public void Check_GarbageFile_IsNotHealthy()
    {
        var path = PathFor("garbage.db");
        File.WriteAllText(path, "this is definitely not a sqlite database, not even close");

        DbIntegrity.IsHealthy(path).Should().BeFalse();
    }

    [Fact]
    public void SafeCopy_UnhealthySource_RefusesAndPreservesDestination()
    {
        var source = PathFor("corrupt.db");
        File.WriteAllText(source, "garbage garbage garbage garbage");
        var dest = CreateHealthyDb("existing-backup.db");

        var ok = DbIntegrity.SafeCopy(source, dest);

        ok.Should().BeFalse("corrupted source must never overwrite a backup");
        DbIntegrity.IsHealthy(dest).Should().BeTrue("the existing healthy backup must survive");
    }

    [Fact]
    public void SafeCopy_HealthySource_ProducesHealthySnapshotWithSameData()
    {
        var source = CreateHealthyDb("live.db", rows: 5);
        var dest = PathFor("backup.db");

        var ok = DbIntegrity.SafeCopy(source, dest);

        ok.Should().BeTrue();
        DbIntegrity.IsHealthy(dest).Should().BeTrue();

        using var conn = new SqliteConnection($"Data Source={dest};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM t;";
        Convert.ToInt32(cmd.ExecuteScalar()).Should().Be(5);
    }

    [Fact]
    public void SafeCopy_OverwritesExistingDestination()
    {
        var source = CreateHealthyDb("live2.db", rows: 7);
        var dest = CreateHealthyDb("old-backup.db", rows: 1);

        DbIntegrity.SafeCopy(source, dest).Should().BeTrue();

        using var conn = new SqliteConnection($"Data Source={dest};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM t;";
        Convert.ToInt32(cmd.ExecuteScalar()).Should().Be(7);
    }

    [Fact]
    public void EnableWal_SwitchesJournalMode_AndIsIdempotent()
    {
        var db = CreateHealthyDb("wal.db");

        DbIntegrity.EnableWal(db);
        DbIntegrity.EnableWal(db); // second call must be harmless

        using var conn = new SqliteConnection($"Data Source={db}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode;";
        cmd.ExecuteScalar()!.ToString().Should().Be("wal");
    }

    [Fact]
    public void TryRecover_HealthySource_ProducesHealthyCopy()
    {
        var source = CreateHealthyDb("recover-src.db", rows: 4);
        var recovered = PathFor("recovered.db");

        DbIntegrity.TryRecover(source, recovered).Should().BeTrue();
        DbIntegrity.IsHealthy(recovered).Should().BeTrue();
    }
}
