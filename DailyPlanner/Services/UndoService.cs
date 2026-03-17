using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DailyPlanner.Services;

/// <summary>
/// Lightweight undo stack with toast UI for one-step undo operations.
/// </summary>
public static class UndoService
{
    private static Action? _undoAction;
    private static DispatcherTimer? _timer;
    private static Border? _activeToast;
    private static int _activeToasts;

    public static void Push(string message, Action undoAction)
    {
        // Dismiss any previous toast
        DismissCurrentToast();

        _undoAction = undoAction;
        ShowUndoToast(message);
    }

    private static void ShowUndoToast(string message)
    {
        if (Application.Current?.Dispatcher is null) return;
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_activeToasts >= 3) return;
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is null) return;

            Interlocked.Increment(ref _activeToasts);

            var toast = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 0, 20, 70),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                MaxWidth = 380,
                Background = Application.Current.Resources["CardBg"] as Brush
                             ?? new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x25)),
                BorderBrush = Application.Current.Resources["AccentBrush"] as Brush
                              ?? new SolidColorBrush(Color.FromRgb(0x7C, 0x5C, 0xFC)),
                BorderThickness = new Thickness(1),
                Opacity = 0,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Application.Current.Resources["AccentColor"] is Color ac ? ac : Color.FromRgb(0x7C, 0x5C, 0xFC),
                    BlurRadius = 20, ShadowDepth = 0, Opacity = 0.3
                }
            };

            var dock = new DockPanel();

            var undoBtn = new Button
            {
                Content = Loc.Get("Undo"),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(12, 4, 12, 4),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Application.Current.Resources["AccentBrush"] as Brush
                             ?? new SolidColorBrush(Color.FromRgb(0x7C, 0x5C, 0xFC)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = Application.Current.Resources["AccentBrush"] as Brush
                              ?? new SolidColorBrush(Color.FromRgb(0x7C, 0x5C, 0xFC)),
            };
            undoBtn.Click += (_, _) =>
            {
                _undoAction?.Invoke();
                _undoAction = null;
                DismissCurrentToast();
            };
            DockPanel.SetDock(undoBtn, Dock.Right);
            dock.Children.Add(undoBtn);

            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = Application.Current.Resources["TextPrimaryBrush"] as Brush
                             ?? new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xF0))
            };
            dock.Children.Add(textBlock);

            toast.Child = dock;
            _activeToast = toast;

            if (mainWindow.Content is Grid grid)
            {
                Panel.SetZIndex(toast, 9998);
                grid.Children.Add(toast);

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                toast.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
                _timer.Tick += (_, _) =>
                {
                    _timer.Stop();
                    _undoAction = null;
                    FadeOutAndRemove(toast, grid);
                };
                _timer.Start();
            }
        });
    }

    private static void DismissCurrentToast()
    {
        if (_activeToast is null) return;
        _timer?.Stop();
        _timer = null;

        if (Application.Current?.Dispatcher is null) return;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow?.Content is Grid grid && _activeToast is not null)
            {
                FadeOutAndRemove(_activeToast, grid);
            }
            _activeToast = null;
        });
    }

    private static void FadeOutAndRemove(Border toast, Grid grid)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        fadeOut.Completed += (_, _) =>
        {
            grid.Children.Remove(toast);
            Interlocked.Decrement(ref _activeToasts);
        };
        toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }
}
