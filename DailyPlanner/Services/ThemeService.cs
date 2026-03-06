using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace DailyPlanner.Services;

public static class ThemeService
{
    private static bool _isDark = true;
    private static Color _accentColor = (Color)ColorConverter.ConvertFromString("#7C5CFC");

    public static bool IsDark => _isDark;

    public static event Action? ThemeChanged;

    public static void ToggleTheme()
    {
        _isDark = !_isDark;
        Apply();
    }

    public static void SetTheme(bool dark)
    {
        _isDark = dark;
        Apply();
    }

    public static void SetAccentColor(string hex)
    {
        _accentColor = (Color)ColorConverter.ConvertFromString(hex);
        var light = Color.FromArgb(255,
            (byte)Math.Min(_accentColor.R + 40, 255),
            (byte)Math.Min(_accentColor.G + 40, 255),
            (byte)Math.Min(_accentColor.B + 40, 255));
        var dark = Color.FromArgb(255,
            (byte)Math.Max(_accentColor.R - 30, 0),
            (byte)Math.Max(_accentColor.G - 30, 0),
            (byte)Math.Max(_accentColor.B - 30, 0));
        ApplyAccentColors(_accentColor, light, dark);
    }

    public static void SetFullAccentColor(string accentHex, string lightHex, string darkHex)
    {
        _accentColor = (Color)ColorConverter.ConvertFromString(accentHex);
        var light = (Color)ColorConverter.ConvertFromString(lightHex);
        var dark = (Color)ColorConverter.ConvertFromString(darkHex);
        ApplyAccentColors(_accentColor, light, dark);
    }

    private static void ApplyAccentColors(Color accent, Color light, Color dark)
    {
        if (Application.Current is null) return;
        var res = Application.Current.Resources;
        res["AccentBrush"] = new SolidColorBrush(accent);
        res["AccentColor"] = accent;
        res["TodayGlowBrush"] = new SolidColorBrush(accent);
        res["AccentLightBrush"] = new SolidColorBrush(light);
        res["AccentLightColor"] = light;
        res["AccentDarkColor"] = dark;
        ThemeChanged?.Invoke();
    }

    private static void Apply()
    {
        ApplicationThemeManager.Apply(
            _isDark ? ApplicationTheme.Dark : ApplicationTheme.Light);

        if (Application.Current is null) return;
        var res = Application.Current.Resources;

        if (_isDark)
        {
            res["CardBg"] = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x25));
            res["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x40));
            res["InputBgBrush"] = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x1C));
            res["MutedBrush"] = new SolidColorBrush(Color.FromRgb(0x58, 0x58, 0x78));
            res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF8));
            res["CheckBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x32, 0x32, 0x5A));
            res["SubtleBgBrush"] = new SolidColorBrush(Colors.White) { Opacity = 0.05 };
            res["FocusBgBrush"] = new SolidColorBrush(_accentColor) { Opacity = 0.06 };
            res["HoverBgBrush"] = new SolidColorBrush(Colors.White) { Opacity = 0.02 };
            res["ProgressTrackBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E));
            res["KeyboardShortcutBg"] = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x40));
            res["SidebarBgBrush"] = new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x12)) { Opacity = 0.8 };
            res["MonthHoverBrush"] = new SolidColorBrush(_accentColor) { Opacity = 0.12 };
            res["MonthPressedBrush"] = new SolidColorBrush(Colors.White) { Opacity = 0.04 };
        }
        else
        {
            res["CardBg"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xFC));
            res["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE8));
            res["InputBgBrush"] = new SolidColorBrush(Colors.White);
            res["MutedBrush"] = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xA0));
            res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E));
            res["CheckBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xD0));
            res["SubtleBgBrush"] = new SolidColorBrush(Colors.Black) { Opacity = 0.04 };
            res["FocusBgBrush"] = new SolidColorBrush(_accentColor) { Opacity = 0.08 };
            res["HoverBgBrush"] = new SolidColorBrush(Colors.Black) { Opacity = 0.02 };
            res["ProgressTrackBrush"] = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0));
            res["KeyboardShortcutBg"] = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0));
            res["SidebarBgBrush"] = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF6)) { Opacity = 0.95 };
            res["MonthHoverBrush"] = new SolidColorBrush(_accentColor) { Opacity = 0.10 };
            res["MonthPressedBrush"] = new SolidColorBrush(Colors.Black) { Opacity = 0.04 };
        }

        ThemeChanged?.Invoke();
    }
}
