using System.IO;
using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Data;
using DailyPlanner.Services;
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
    private void BackupDatabase()
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"DailyPlanner_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
            DefaultExt = ".db",
            Filter = "SQLite DB (*.db)|*.db"
        };
        if (dialog.ShowDialog() != true) return;

        var dbPath = PlannerDbContextFactory.DbPath;
        if (File.Exists(dbPath))
            File.Copy(dbPath, dialog.FileName, true);
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

        // Validate the backup file is a valid SQLite database
        try
        {
            await using var testDb = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dialog.FileName};Mode=ReadOnly");
            await testDb.OpenAsync();
            await testDb.CloseAsync();
        }
        catch (Exception ex)
        {
            Log.Error("MainVM", $"Invalid backup: {ex.Message}");
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
        }
        catch (Exception ex)
        {
            Log.Error("MainVM", $"Restore failed: {ex.Message}");
            // Restore from safety backup if copy failed
            if (File.Exists(backupPath))
                File.Copy(backupPath, dbPath, true);
            NotificationService.ShowToast(Loc.Get("RestoreTitle"), Loc.Get("RestoreError"));
            return;
        }

        await LoadMonthAsync();
        await LoadTemplatesAsync();
        await LoadRemindersAsync();
        await LoadMeetingsAsync();
        NotificationService.ShowToast(Loc.Get("RestoreTitle"), Loc.Get("RestoreSuccess"));
    }
}
