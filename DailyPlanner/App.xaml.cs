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
        DispatcherUnhandledException += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[App] Unhandled: {e.Exception}");
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

        base.OnStartup(e);

        // Velopack: handle install/uninstall/update hooks (creates shortcuts, etc.)
        VelopackApp.Build().Run();

        // Apply theme resources (must run AFTER WPF-UI loads but BEFORE window renders)
        ThemeService.Apply();

        // Show splash
        var splash = new Window
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
        title.Inlines.Add(new System.Windows.Documents.Run("Daily") { Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x5C, 0xFC)) });
        title.Inlines.Add(new System.Windows.Documents.Run(" & ") { Foreground = Brushes.White });
        title.Inlines.Add(new System.Windows.Documents.Run("Financial") { Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x5C, 0xFC)) });
        title.Inlines.Add(new System.Windows.Documents.Run(" Planner") { Foreground = Brushes.White });

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
            Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x5C, 0xFC)),
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E)),
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
                    System.IO.File.Copy(dbPath, System.IO.Path.Combine(backupDir, backupName), true);

                    // Rotate: keep only last 5 backups
                    var old = System.IO.Directory.GetFiles(backupDir, "planner_*.db")
                        .OrderByDescending(f => System.IO.File.GetCreationTimeUtc(f))
                        .Skip(5);
                    foreach (var f in old)
                        try { System.IO.File.Delete(f); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[App] Backup cleanup: {ex.Message}"); }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[App] Backup failed: {ex.Message}"); }
        });

        // Initialize DB
        try
        {
            await Task.Run(() =>
            {
                using var db = PlannerDbContextFactory.Create();
                db.Database.Migrate();
            });
        }
        catch (Exception ex)
        {
            splash.Close();
            System.Windows.MessageBox.Show(
                string.Format(Loc.Get("DbError"), ex.Message),
                "Daily & Financial Planner",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        splash.Close();
    }
}
