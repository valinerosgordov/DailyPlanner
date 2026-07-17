using System.IO;
using Microsoft.Data.Sqlite;

namespace DailyPlanner.Services;

/// <summary>
/// SQLite database integrity checks, safe backup gating, and lightweight recovery.
///
/// Why this exists: the app's auto-backup on startup was a raw <see cref="File.Copy"/>.
/// When the live DB became corrupted, every subsequent backup was a copy of the
/// corrupted file — the user lost all healthy backups within the retention window
/// (last 5). This service refuses to back up unhealthy databases and offers a
/// VACUUM INTO based recovery path.
/// </summary>
public static class DbIntegrity
{
    public const string OkResult = "ok";

    /// <summary>
    /// Runs <c>PRAGMA integrity_check</c> on a SQLite file. Returns "ok" if the
    /// database is healthy, otherwise the first error line SQLite emitted, or a
    /// short diagnostic if the file could not be opened at all.
    /// </summary>
    public static string Check(string dbPath)
    {
        if (string.IsNullOrWhiteSpace(dbPath)) return "missing-path";
        if (!File.Exists(dbPath)) return "missing-file";

        try
        {
            using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var result = cmd.ExecuteScalar()?.ToString();
            return string.IsNullOrEmpty(result) ? "no-result" : result;
        }
        catch (SqliteException ex)
        {
            return $"sqlite-error: {ex.SqliteErrorCode} {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"error: {ex.Message}";
        }
    }

    public static bool IsHealthy(string dbPath) => Check(dbPath) == OkResult;

    /// <summary>
    /// Snapshots <paramref name="sourcePath"/> to <paramref name="destPath"/> only when
    /// the source passes <c>PRAGMA integrity_check</c>. If the source is unhealthy,
    /// the copy is refused and the existing destination (if any) is left intact.
    ///
    /// The snapshot itself is <c>VACUUM INTO</c>, not <see cref="File.Copy"/>: a raw
    /// copy of a live DB can capture a mid-write state and silently misses the
    /// committed tail still sitting in the -wal sidecar.
    /// </summary>
    /// <returns>true if the snapshot was produced; false otherwise.</returns>
    public static bool SafeCopy(string sourcePath, string destPath)
    {
        var status = Check(sourcePath);
        if (status != OkResult)
        {
            Log.Error("DbIntegrity",
                $"Refusing backup: source DB unhealthy. status='{Truncate(status, 200)}', source='{sourcePath}'");
            return false;
        }

        try
        {
            // VACUUM INTO refuses to overwrite an existing file
            if (File.Exists(destPath)) File.Delete(destPath);

            using var conn = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            // VACUUM INTO does not accept parameter binding for the path literal.
            // Paths come from app config or the user's own Save dialog, not remote input.
            cmd.CommandText = $"VACUUM INTO '{destPath.Replace("'", "''")}';";
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("DbIntegrity", $"SafeCopy failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Switches the database to persistent WAL journal mode. Readers stop blocking
    /// writers (debounced saves, reminder timer ticks, startup rate refresh), and
    /// the checkpoint/recovery plumbing in this class becomes meaningful.
    /// Idempotent — the mode is stored inside the DB file.
    /// </summary>
    public static void EnableWal(string dbPath)
    {
        if (!File.Exists(dbPath)) return;
        try
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL;";
            cmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
            Log.Error("DbIntegrity", $"EnableWal failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to recover salvageable data from a corrupted SQLite using
    /// <c>VACUUM INTO</c>. The new file gets only the live, navigable rows;
    /// orphaned pages are dropped. Verifies the result with integrity_check.
    /// </summary>
    /// <returns>true if a healthy recovered DB was produced.</returns>
    public static bool TryRecover(string corruptPath, string recoveredPath)
    {
        if (!File.Exists(corruptPath))
        {
            Log.Error("DbIntegrity", $"TryRecover: source missing: {corruptPath}");
            return false;
        }

        try { if (File.Exists(recoveredPath)) File.Delete(recoveredPath); } catch { /* best-effort */ }

        try
        {
            using var conn = new SqliteConnection($"Data Source={corruptPath};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            // VACUUM INTO does not accept parameter binding for the path literal.
            // Path is built locally (not user input), so manual quoting is acceptable here.
            cmd.CommandText = $"VACUUM INTO '{recoveredPath.Replace("'", "''")}';";
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Log.Error("DbIntegrity", $"VACUUM INTO failed: {ex.Message}");
            return false;
        }

        var status = Check(recoveredPath);
        if (status != OkResult)
        {
            Log.Error("DbIntegrity", $"Recovered DB still unhealthy: {Truncate(status, 200)}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Forces a WAL checkpoint to flush the write-ahead log into the main DB file.
    /// Reduces the window in which a crash can leave the WAL desynced from main.
    /// </summary>
    public static void Checkpoint(string dbPath)
    {
        if (!File.Exists(dbPath)) return;
        try
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Log.Error("DbIntegrity", $"Checkpoint failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns the most recent backup whose integrity_check passes, scanning
    /// <c>planner_*.db</c> files in <paramref name="backupDir"/> from newest to
    /// oldest. Returns null if no healthy backup exists.
    /// </summary>
    public static string? FindLatestHealthyBackup(string backupDir)
    {
        if (!Directory.Exists(backupDir)) return null;

        var candidates = Directory.GetFiles(backupDir, "planner_*.db")
            .OrderByDescending(File.GetCreationTimeUtc);

        foreach (var path in candidates)
        {
            if (IsHealthy(path)) return path;
        }
        return null;
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max) + "...");
}