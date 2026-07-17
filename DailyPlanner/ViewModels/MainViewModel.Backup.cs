using System.IO;
using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Data;
using DailyPlanner.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace DailyPlanner.ViewModels;

/// <summary>
/// Manual backup / restore commands wired to Ctrl+K palette + settings page.
/// Auto-backup runs in App.xaml.cs (DbIntegrity.SafeCopy on startup); these are
/// the user-initiated counterparts.
///
/// Split out of MainViewModel.cs — partial class, identical public API,
/// preserves all existing XAML bindings (BackupDatabaseCommand /
/// RestoreDatabaseCommand). Reduces the main file from 820 → ~750 lines.
/// </summary>
public partial class MainViewModel
{
    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"DailyPlanner_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
            DefaultExt = ".db",
            Filter = "SQLite DB (*.db)|*.db"
        };
        if (dialog.ShowDialog() != true) return;

        var dbPath = PlannerDbContextFactory.DbPath;
        if (!File.Exists(dbPath)) return;

        // SafeCopy = integrity gate + VACUUM INTO: a raw File.Copy of the live DB
        // could snapshot mid-write and silently miss the WAL tail.
        var ok = await Task.Run(() => DbIntegrity.SafeCopy(dbPath, dialog.FileName));
        NotificationService.ShowToast(
            Loc.Get("BackupTitle"),
            Loc.Get(ok ? "BackupSuccess" : "BackupCorruptError"));
    }

    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        var dialog = new OpenFileDialog
        {
            DefaultExt = ".db",
            Filter = "SQLite DB (*.db)|*.db"
        };
        if (dialog.ShowDialog() != true) return;

        var dbPath = PlannerDbContextFactory.DbPath;

        // Full PRAGMA integrity_check — the previous "does it open" probe let a
        // corrupted-but-openable file overwrite the live DB.
        var healthy = await Task.Run(() => DbIntegrity.IsHealthy(dialog.FileName));
        if (!healthy)
        {
            Log.Error("MainVM", $"Restore refused: '{dialog.FileName}' failed integrity_check");
            NotificationService.ShowToast(Loc.Get("RestoreTitle"), Loc.Get("RestoreInvalidDb"));
            return;
        }

        // Create safety backup before overwriting
        var backupPath = dbPath + ".pre-restore";
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            if (File.Exists(dbPath))
                File.Copy(dbPath, backupPath, true);

            File.Copy(dialog.FileName, dbPath, true);

            // WAL/SHM files may be briefly locked after pool clear
            foreach (var suffix in new[] { "-wal", "-shm" })
            {
                var path = dbPath + suffix;
                if (!File.Exists(path)) continue;
                try { File.Delete(path); }
                catch (IOException) { /* SQLite will recreate if needed */ }
            }

            // A backup made by an older app version has an older schema — bring it
            // up to date now, or every query until restart hits "no such column".
            await Task.Run(() =>
            {
                using var db = PlannerDbContextFactory.Create();
                db.Database.Migrate();
            });
        }
        catch (Exception ex)
        {
            Log.Error("MainVM", $"Restore failed: {ex.Message}");
            // Roll back to the pre-restore snapshot if copy or migration failed
            if (File.Exists(backupPath))
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                File.Copy(backupPath, dbPath, true);
            }
            NotificationService.ShowToast(Loc.Get("RestoreTitle"), Loc.Get("RestoreError"));
            return;
        }

        await LoadMonthAsync();
        await LoadTemplatesAsync();
        await LoadRemindersAsync();
        await LoadMeetingsAsync();

        // Success — retire the safety copy; startup backups keep their own history.
        try { if (File.Exists(backupPath)) File.Delete(backupPath); }
        catch (IOException) { /* best-effort cleanup */ }

        NotificationService.ShowToast(Loc.Get("RestoreTitle"), Loc.Get("RestoreSuccess"));
    }
}
