using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DailyPlanner.Services;

public static class NotificationService
{
    private static readonly DispatcherTimer _checkTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    private static readonly HashSet<string> _notifiedToday = new(StringComparer.Ordinal);
    private static readonly object _lock = new();
    private static DateOnly _lastCheckDate;
    private static int _activeToasts;

    public static event Action<string, string>? NotificationTriggered;

    public static void Start()
    {
        _lastCheckDate = DateOnly.FromDateTime(DateTime.Today);
        _checkTimer.Tick += OnCheck;
        _checkTimer.Start();
    }

    public static void Stop()
    {
        _checkTimer.Tick -= OnCheck;
        _checkTimer.Stop();
    }

    private static void OnCheck(object? sender, EventArgs e)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        lock (_lock)
        {
            if (today != _lastCheckDate)
            {
                _notifiedToday.Clear();
                _lastCheckDate = today;
            }
        }
        // Notification check delegated to MainViewModel
    }

    public static void ShowToast(string title, string message)
    {
        var key = $"{title}:{message}";
        lock (_lock) { if (!_notifiedToday.Add(key)) return; }
        NotificationTriggered?.Invoke(title, message);

        if (Application.Current?.Dispatcher is null) return;
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_activeToasts >= 3) return; // Limit concurrent toasts
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is null) return;

            Interlocked.Increment(ref _activeToasts);
            var toast = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 20, 20),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                MaxWidth = 320,
                Background = Application.Current.Resources["CardBg"] as System.Windows.Media.Brush
                             ?? new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x25)),
                BorderBrush = Application.Current.Resources["AccentBrush"] as System.Windows.Media.Brush
                              ?? new SolidColorBrush(Color.FromRgb(0x7C, 0x5C, 0xFC)),
                BorderThickness = new Thickness(1),
                Opacity = 0,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Application.Current.Resources["AccentColor"] is Color ac ? ac : Color.FromRgb(0x7C, 0x5C, 0xFC),
                    BlurRadius = 20, ShadowDepth = 0, Opacity = 0.3
                }
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title, FontWeight = FontWeights.SemiBold, FontSize = 14,
                Foreground = Application.Current.Resources["AccentBrush"] as System.Windows.Media.Brush
                             ?? new SolidColorBrush(Color.FromRgb(0x7C, 0x5C, 0xFC)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            stack.Children.Add(new TextBlock
            {
                Text = message, FontSize = 12, TextWrapping = TextWrapping.Wrap,
                Foreground = Application.Current.Resources["TextPrimaryBrush"] as System.Windows.Media.Brush
                             ?? new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xF0))
            });
            toast.Child = stack;

            if (mainWindow.Content is Grid grid)
            {
                Panel.SetZIndex(toast, 9999);
                grid.Children.Add(toast);

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                toast.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                    fadeOut.Completed += (_, _) =>
                    {
                        grid.Children.Remove(toast);
                        Interlocked.Decrement(ref _activeToasts);
                    };
                    toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                };
                timer.Start();
            }
        });
    }
}
