using System.Windows;
using System.Windows.Media;
using DailyPlanner.Data;
using DailyPlanner.Services;
using Microsoft.EntityFrameworkCore;
using Velopack;

namespace DailyPlanner;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        if (!SingleInstanceGuard.TryAcquire(out _))
        {
            SingleInstanceGuard.SignalExistingInstance();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, e) =>
        {
            Log.Error("App", $"Unhandled: {e.Exception}");
            try
            {
                var logPath = System.IO.Path.Combine(PlannerDbContextFactory.AppDataFolder, "crash.log");
                System.IO.File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}\n\n");
            }
            catch { }
            System.Windows.MessageBox.Show(
                $"An unexpected error occurred:\n{e.Exception.Message}",
                "Daily & Financial Planner", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        Exit += (_, _) => SingleInstanceGuard.Release();

        base.OnStartup(e);

        Window? splash = null;
        try
        {

            VelopackApp.Build().Run();

            ServiceHost.Configure();

            // Must run AFTER WPF-UI loads but BEFORE window renders
            ThemeService.Apply();

            SingleInstanceGuard.StartActivationListener(ActivateMainWindow);

            splash = new Window
            {
                Width = 380,
                Height = 220,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };

            var border = new System.Windows.Controls.Border
            {
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromRgb(0x10, 0x10, 0x1A)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x40)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(32)
            };

            var stack = new System.Windows.Controls.StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var title = new System.Windows.Controls.TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            title.Inlines.Add(new System.Windows.Documents.Run("Daily") { Foreground = Brushes.White });
            title.Inlines.Add(new System.Windows.Documents.Run(" & ") { Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x96)) });
            title.Inlines.Add(new System.Windows.Documents.Run("Financial") { Foreground = Brushes.White });
            title.Inlines.Add(new System.Windows.Documents.Run(" Planner") { Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x96)) });

            var subtitle = new System.Windows.Controls.TextBlock
            {
                Text = Loc.Get("Loading"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0x58, 0x78)),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var progress = new System.Windows.Controls.ProgressBar
            {
                IsIndeterminate = true,
                Height = 3,
                Margin = new Thickness(20, 16, 20, 0),
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x24)),
                BorderThickness = new Thickness(0)
            };

            stack.Children.Add(title);
            stack.Children.Add(subtitle);
            stack.Children.Add(progress);
            border.Child = stack;
            splash.Content = border;
            splash.Show();

            // Auto-backup before migration (keep last 5)
            await Task.Run(() =>
            {
                try
                {
                    var dbPath = PlannerDbContextFactory.DbPath;
                    if (System.IO.File.Exists(dbPath))
                    {
                        var backupDir = System.IO.Path.Combine(PlannerDbContextFactory.AppDataFolder, "backups");
                        System.IO.Directory.CreateDirectory(backupDir);
                        var backupName = $"planner_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                        var backupPath = System.IO.Path.Combine(backupDir, backupName);

                        // SafeCopy refuses to back up an unhealthy DB. This protects the
                        // retention window — without this gate every backup would just be
                        // a copy of the same corrupted file, wiping good history.
                        if (DbIntegrity.SafeCopy(dbPath, backupPath))
                        {
                            var old = System.IO.Directory.GetFiles(backupDir, "planner_*.db")
                                .OrderByDescending(f => System.IO.File.GetCreationTimeUtc(f))
                                .Skip(5);
                            foreach (var f in old)
                                try { System.IO.File.Delete(f); } catch (Exception ex) { Log.Error("App", $"Backup cleanup: {ex.Message}"); }
                        }
                        // else: SafeCopy already logged the reason; old healthy backups stay.
                    }
                }
                catch (Exception ex) { Log.Error("App", $"Backup failed: {ex.Message}"); }
            });

            // Startup integrity check — catches corruption BEFORE EF tries to migrate
            // (which would just throw a confusing 'malformed' exception every launch).
            // If the live DB is bad, offer to recover via VACUUM INTO or fall back to
            // the most recent healthy backup.
            try
            {
                var dbPath = PlannerDbContextFactory.DbPath;
                if (System.IO.File.Exists(dbPath) && !DbIntegrity.IsHealthy(dbPath))
                {
                    splash.Close();
                    if (!await TryRecoverDatabaseInteractiveAsync(dbPath))
                    {
                        Shutdown(1);
                        return;
                    }
                    splash = null;
                }
            }
            catch (Exception ex) { Log.Error("App", $"Integrity check failed: {ex.Message}"); }

            try
            {
                await Task.Run(() =>
                {
                    using var db = PlannerDbContextFactory.Create();
                    db.Database.Migrate();
                });

                // WAL checkpoint right after migration: flushes any -wal residue into
                // the main file so a crash window doesn't leave the on-disk image
                // referencing log pages that may not have been fsync'd.
                DbIntegrity.Checkpoint(PlannerDbContextFactory.DbPath);
            }
            catch (Exception ex)
            {
                splash?.Close();
                System.Windows.MessageBox.Show(
                    string.Format(Loc.Get("DbError"), ex.Message),
                    "Daily & Financial Planner",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            splash?.Close();

            // Fire-and-forget: pull today's currency rates from CBR. Not awaited
            // because finance aggregates fall back gracefully when rates are
            // missing (logged + amount returned unconverted), and a slow CBR
            // response should never delay app launch.
            _ = Task.Run(async () =>
            {
                try { await ServiceHost.Get<ExchangeRateService>().RefreshAsync(); }
                catch (Exception ex) { Log.Error("App", $"ExchangeRate refresh failed: {ex.Message}"); }
            });
        }
        catch (Exception ex)
        {
            Log.Error("App", $"Startup failed: {ex}");
            try { splash?.Close(); } catch { }
            System.Windows.MessageBox.Show(
                $"Startup failed:\n{ex.Message}",
                "Daily & Financial Planner",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// Shown when the live DB fails PRAGMA integrity_check at startup. Offers two
    /// non-destructive paths: in-place recovery via VACUUM INTO, or restore from the
    /// most recent healthy backup. The corrupted file is always preserved as
    /// .corrupt-{timestamp} alongside the live DB before any replacement.
    /// </summary>
    private static async Task<bool> TryRecoverDatabaseInteractiveAsync(string dbPath)
    {
        var backupDir = System.IO.Path.Combine(PlannerDbContextFactory.AppDataFolder, "backups");
        var healthyBackup = DbIntegrity.FindLatestHealthyBackup(backupDir);

        var msg = string.Format(Loc.Get("RecoveryMessage"),
            healthyBackup is null ? Loc.Get("RecoveryNoBackup") : System.IO.Path.GetFileName(healthyBackup));

        // Yes  = attempt VACUUM INTO recovery (preserves all live data we can salvage)
        // No   = restore from latest healthy backup (older but guaranteed clean)
        // Cancel = quit so the user can decide what to do manually
        var choice = System.Windows.MessageBox.Show(
            msg,
            Loc.Get("RecoveryTitle"),
            healthyBackup is null ? MessageBoxButton.YesNoCancel : MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Yes);

        if (choice == MessageBoxResult.Cancel) return false;

        var corruptArchive = $"{dbPath}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
        try { System.IO.File.Copy(dbPath, corruptArchive, true); }
        catch (Exception ex) { Log.Error("App", $"Could not archive corrupt DB: {ex.Message}"); }

        if (choice == MessageBoxResult.Yes)
        {
            var recovered = $"{dbPath}.recovered";
            var ok = await Task.Run(() => DbIntegrity.TryRecover(dbPath, recovered));
            if (!ok)
            {
                System.Windows.MessageBox.Show(Loc.Get("RecoveryFailed"),
                    Loc.Get("RecoveryTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            try
            {
                System.IO.File.Delete(dbPath);
                System.IO.File.Move(recovered, dbPath);
                // Stale WAL/SHM from the corrupted DB must go — they reference pages
                // that no longer exist in the recovered file.
                foreach (var sidecar in new[] { dbPath + "-wal", dbPath + "-shm" })
                    try { if (System.IO.File.Exists(sidecar)) System.IO.File.Delete(sidecar); } catch { }
            }
            catch (Exception ex)
            {
                Log.Error("App", $"Recovered swap failed: {ex.Message}");
                System.Windows.MessageBox.Show(Loc.Get("RecoveryFailed"),
                    Loc.Get("RecoveryTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            System.Windows.MessageBox.Show(Loc.Get("RecoverySuccess"),
                Loc.Get("RecoveryTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }

        // choice == No: restore from healthy backup
        if (healthyBackup is null)
        {
            System.Windows.MessageBox.Show(Loc.Get("RecoveryNoBackup"),
                Loc.Get("RecoveryTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        try
        {
            System.IO.File.Copy(healthyBackup, dbPath, true);
            foreach (var sidecar in new[] { dbPath + "-wal", dbPath + "-shm" })
                try { if (System.IO.File.Exists(sidecar)) System.IO.File.Delete(sidecar); } catch { }
        }
        catch (Exception ex)
        {
            Log.Error("App", $"Backup restore failed: {ex.Message}");
            System.Windows.MessageBox.Show(Loc.Get("RecoveryFailed"),
                Loc.Get("RecoveryTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        System.Windows.MessageBox.Show(Loc.Get("RecoverySuccess"),
            Loc.Get("RecoveryTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        return true;
    }
    private void ActivateMainWindow()
    {
        if (MainWindow is null) return;

        if (!MainWindow.IsVisible) MainWindow.Show();
        if (MainWindow.WindowState == WindowState.Minimized) MainWindow.WindowState = WindowState.Normal;

        MainWindow.Activate();
        MainWindow.Topmost = true;
        MainWindow.Topmost = false;
        MainWindow.Focus();
    }
}
