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
        title.Inlines.Add(new System.Windows.Documents.Run("Daily ") { Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x5C, 0xFC)) });
        title.Inlines.Add(new System.Windows.Documents.Run("Planner") { Foreground = Brushes.White });

        var subtitle = new System.Windows.Controls.TextBlock
        {
            Text = "Загрузка данных...",
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
                $"Ошибка инициализации базы данных:\n{ex.Message}",
                "Daily Planner",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        splash.Close();
    }
}
