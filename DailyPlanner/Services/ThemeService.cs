using System.IO;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace DailyPlanner.Services;

public record ThemePalette(
    string Name,
    bool IsDark,
    Color Accent, Color AccentLight, Color AccentDark,
    Color PageBg, Color CardBg, Color CardBorder,
    Color SidebarBg, Color InputBg, Color SubtleBg,
    Color Text, Color Muted, Color CheckBorder,
    Color HoverBg, Color ProgressTrack, Color KeyboardBg,
    Color Success, Color Warning, Color Danger, Color Info);

public static class ThemeService
{
    private static string _currentPalette = LoadSavedPalette();

    public static string CurrentPalette => _currentPalette;

    private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DailyPlanner", "theme.txt");

    private static string LoadSavedPalette()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var name = File.ReadAllText(SettingsPath).Trim();
                // Migrate: only "Dark" and "Light" are supported now
                if (name is "Light" or "Светлая") return "Light";
                if (Palettes.ContainsKey(name)) return name;
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ThemeService] Load failed: {ex.Message}"); }
        return "Dark";
    }

    private static void SavePalette(string name)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath, name);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ThemeService] Save failed: {ex.Message}"); }
    }

    private static Color Argb(byte a, string hex)
    {
        var c = Hex(hex);
        return Color.FromArgb(a, c.R, c.G, c.B);
    }

    public static readonly Dictionary<string, ThemePalette> Palettes = new()
    {
        // Pure Monochrome Dark — pitch black + white accents, no colored tint
        ["Dark"] = new("Dark", true,
            Accent: Hex("#FFFFFF"), AccentLight: Hex("#F5F5F5"), AccentDark: Hex("#D4D4D4"),
            PageBg: Argb(0x00, "#000000"),           // transparent — Mica shows through
            CardBg: Argb(0x0F, "#FFFFFF"),           // 6% white frosted glass
            CardBorder: Argb(0x1A, "#FFFFFF"),       // 10% white edge
            SidebarBg: Argb(0x66, "#000000"),        // 40% pure black panel
            InputBg: Argb(0x14, "#FFFFFF"),          // 8% white input fill
            SubtleBg: Argb(0x08, "#FFFFFF"),         // 3% white subtle
            Text: Hex("#FFFFFF"), Muted: Hex("#8E8E96"), CheckBorder: Argb(0x33, "#FFFFFF"),
            HoverBg: Argb(0x1A, "#FFFFFF"), ProgressTrack: Argb(0x14, "#FFFFFF"),
            KeyboardBg: Argb(0x14, "#FFFFFF"),
            Success: Hex("#FFFFFF"), Warning: Hex("#E5E5E5"), Danger: Hex("#FF9999"), Info: Hex("#B8B8B8")),

        // Pure Monochrome Light
        ["Light"] = new("Light", false,
            Accent: Hex("#000000"), AccentLight: Hex("#1A1A1A"), AccentDark: Hex("#333333"),
            PageBg: Hex("#FFFFFF"), CardBg: Hex("#FAFAFA"), CardBorder: Hex("#E5E5E5"),
            SidebarBg: Hex("#F5F5F5"), InputBg: Hex("#F5F5F5"), SubtleBg: Hex("#EFEFEF"),
            Text: Hex("#000000"), Muted: Hex("#6B6B6B"), CheckBorder: Hex("#D4D4D4"),
            HoverBg: Hex("#EBEBEB"), ProgressTrack: Hex("#E5E5E5"), KeyboardBg: Hex("#EFEFEF"),
            Success: Hex("#000000"), Warning: Hex("#4A4A4A"), Danger: Hex("#991B1B"), Info: Hex("#525252")),
    };

    public static void ApplyPalette(string name)
    {
        if (!Palettes.TryGetValue(name, out var p)) return;
        _currentPalette = name;
        SavePalette(name);

        ApplicationThemeManager.Apply(p.IsDark ? ApplicationTheme.Dark : ApplicationTheme.Light);

        if (Application.Current is null) return;
        var res = Application.Current.Resources;

        // Accent colors
        res["AccentBrush"] = new SolidColorBrush(p.Accent);
        res["AccentColor"] = p.Accent;
        res["TodayGlowBrush"] = new SolidColorBrush(p.Accent);
        res["AccentLightBrush"] = new SolidColorBrush(p.AccentLight);
        res["AccentLightColor"] = p.AccentLight;
        res["AccentDarkColor"] = p.AccentDark;

        // Semantic colors
        res["SuccessBrush"] = new SolidColorBrush(p.Success);
        res["SuccessColor"] = p.Success;
        res["WarningBrush"] = new SolidColorBrush(p.Warning);
        res["WarningColor"] = p.Warning;
        res["DangerBrush"] = new SolidColorBrush(p.Danger);
        res["DangerColor"] = p.Danger;
        res["InfoBrush"] = new SolidColorBrush(p.Info);
        res["InfoColor"] = p.Info;

        // Background layers
        res["PageBgBrush"] = new SolidColorBrush(p.PageBg);
        res["CardBg"] = new SolidColorBrush(p.CardBg);
        res["CardBorderBrush"] = new SolidColorBrush(p.CardBorder);
        res["SidebarBgBrush"] = new SolidColorBrush(p.SidebarBg);
        res["InputBgBrush"] = new SolidColorBrush(p.InputBg);
        res["SubtleBgBrush"] = new SolidColorBrush(p.SubtleBg);

        // Text
        res["TextPrimaryBrush"] = new SolidColorBrush(p.Text);
        res["MutedBrush"] = new SolidColorBrush(p.Muted);
        res["CheckBorderBrush"] = new SolidColorBrush(p.CheckBorder);

        // Aliases used by redesigned screens (Inbox, Finance etc.)
        res["CardBackgroundBrush"] = new SolidColorBrush(p.CardBg);
        res["BackgroundBrush"] = new SolidColorBrush(p.PageBg);
        res["BorderBrush"] = new SolidColorBrush(p.CardBorder);
        var secondary = p.Text;
        res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromArgb(0xCC, secondary.R, secondary.G, secondary.B));
        res["TextTertiaryBrush"] = new SolidColorBrush(Color.FromArgb(0x88, secondary.R, secondary.G, secondary.B));

        // Interactive states
        res["FocusBgBrush"] = new SolidColorBrush(p.Accent) { Opacity = p.IsDark ? 0.1 : 0.08 };
        res["HoverBgBrush"] = new SolidColorBrush(p.HoverBg) { Opacity = p.IsDark ? 0.3 : 0.7 };
        res["MonthHoverBrush"] = new SolidColorBrush(p.Accent) { Opacity = p.IsDark ? 0.15 : 0.10 };
        res["MonthPressedBrush"] = new SolidColorBrush(p.HoverBg) { Opacity = p.IsDark ? 0.4 : 0.6 };

        // Other
        res["ProgressTrackBrush"] = new SolidColorBrush(p.ProgressTrack);
        res["KeyboardShortcutBg"] = new SolidColorBrush(p.KeyboardBg);
    }

    public static void Apply() => ApplyPalette(_currentPalette);
}
