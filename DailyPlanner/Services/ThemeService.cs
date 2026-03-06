using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace DailyPlanner.Services;

public static class ThemeService
{
    private static bool _isDark = true;
    private static Color _accentColor = (Color)ColorConverter.ConvertFromString("#cba6f7");

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
        try { _accentColor = (Color)ColorConverter.ConvertFromString(hex); }
        catch (FormatException) { return; }
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
        res["FocusBgBrush"] = new SolidColorBrush(accent) { Opacity = 0.1 };
        res["MonthHoverBrush"] = new SolidColorBrush(accent) { Opacity = 0.15 };
        ThemeChanged?.Invoke();
    }

    public static void Apply()
    {
        ApplicationThemeManager.Apply(
            _isDark ? ApplicationTheme.Dark : ApplicationTheme.Light);

        if (Application.Current is null) return;
        var res = Application.Current.Resources;

        if (_isDark)
        {
            // Catppuccin Mocha palette
            // Base: #1e1e2e, Mantle: #181825, Crust: #11111b
            // Surface 0: #313244, Surface 1: #45475a, Surface 2: #585b70
            // Overlay 0: #6c7086, Text: #cdd6f4, Subtext 1: #bac2de
            res["CardBg"] = new SolidColorBrush(Color.FromRgb(0x31, 0x32, 0x44));             // Surface 0
            res["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5a));    // Surface 1
            res["InputBgBrush"] = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x2e));       // Base
            res["MutedBrush"] = new SolidColorBrush(Color.FromRgb(0x6c, 0x70, 0x86));         // Overlay 0
            res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xcd, 0xd6, 0xf4));   // Text
            res["CheckBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x58, 0x5b, 0x70));   // Surface 2
            res["SubtleBgBrush"] = new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5a));      // Surface 1
            res["FocusBgBrush"] = new SolidColorBrush(_accentColor) { Opacity = 0.1 };
            res["HoverBgBrush"] = new SolidColorBrush(Color.FromRgb(0x58, 0x5b, 0x70)) { Opacity = 0.3 };
            res["ProgressTrackBrush"] = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x2e)); // Base
            res["KeyboardShortcutBg"] = new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5a)); // Surface 1
            res["SidebarBgBrush"] = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x1b));     // Crust
            res["MonthHoverBrush"] = new SolidColorBrush(_accentColor) { Opacity = 0.15 };
            res["MonthPressedBrush"] = new SolidColorBrush(Color.FromRgb(0x58, 0x5b, 0x70)) { Opacity = 0.4 };
        }
        else
        {
            // Catppuccin Latte palette (light theme)
            // Base: #eff1f5, Mantle: #e6e9ef, Crust: #dce0e8
            // Surface 0: #ccd0da, Surface 1: #bcc0cc, Surface 2: #acb0be
            // Overlay 0: #9ca0b0, Text: #4c4f69, Subtext 1: #5c5f77
            res["CardBg"] = new SolidColorBrush(Colors.White);
            res["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xcc, 0xd0, 0xda));    // Surface 0
            res["InputBgBrush"] = new SolidColorBrush(Color.FromRgb(0xef, 0xf1, 0xf5));       // Base
            res["MutedBrush"] = new SolidColorBrush(Color.FromRgb(0x9c, 0xa0, 0xb0));         // Overlay 0
            res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x4c, 0x4f, 0x69));   // Text
            res["CheckBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xbc, 0xc0, 0xcc));   // Surface 1
            res["SubtleBgBrush"] = new SolidColorBrush(Color.FromRgb(0xef, 0xf1, 0xf5));      // Base
            res["FocusBgBrush"] = new SolidColorBrush(_accentColor) { Opacity = 0.1 };
            res["HoverBgBrush"] = new SolidColorBrush(Color.FromRgb(0xcc, 0xd0, 0xda)) { Opacity = 0.5 };
            res["ProgressTrackBrush"] = new SolidColorBrush(Color.FromRgb(0xe6, 0xe9, 0xef)); // Mantle
            res["KeyboardShortcutBg"] = new SolidColorBrush(Color.FromRgb(0xe6, 0xe9, 0xef)); // Mantle
            res["SidebarBgBrush"] = new SolidColorBrush(Color.FromRgb(0xe6, 0xe9, 0xef));     // Mantle
            res["MonthHoverBrush"] = new SolidColorBrush(_accentColor) { Opacity = 0.12 };
            res["MonthPressedBrush"] = new SolidColorBrush(Color.FromRgb(0xcc, 0xd0, 0xda)) { Opacity = 0.5 };
        }

        ThemeChanged?.Invoke();
    }
}
