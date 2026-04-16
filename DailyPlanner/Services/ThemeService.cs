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
        // Liquid Glass Dark — glassmorphism over Mica backdrop, violet accent
        ["Dark"] = new("Dark", true,
            Accent: Hex("#8B5CF6"), AccentLight: Hex("#C4B5FD"), AccentDark: Hex("#6D28D9"),
            PageBg: Argb(0x00, "#0E0E12"),           // transparent — Mica shows through
            CardBg: Argb(0x14, "#FFFFFF"),           // 8% white frosted glass
            CardBorder: Argb(0x26, "#FFFFFF"),       // 15% white highlight edge
            SidebarBg: Argb(0x4D, "#0A0A0F"),        // 30% darker panel
            InputBg: Argb(0x1F, "#FFFFFF"),          // 12% white input fill
            SubtleBg: Argb(0x0A, "#FFFFFF"),         // 4% white subtle
            Text: Hex("#F5F5FA"), Muted: Hex("#9CA0B8"), CheckBorder: Argb(0x40, "#FFFFFF"),
            HoverBg: Argb(0x1F, "#FFFFFF"), ProgressTrack: Argb(0x1A, "#FFFFFF"),
            KeyboardBg: Argb(0x1F, "#FFFFFF"),
            Success: Hex("#34D399"), Warning: Hex("#FBBF24"), Danger: Hex("#F87171"), Info: Hex("#60A5FA")),

        // Modern Light Minimal — same violet accent
        ["Light"] = new("Light", false,
            Accent: Hex("#7C3AED"), AccentLight: Hex("#A78BFA"), AccentDark: Hex("#6D28D9"),
            PageBg: Hex("#FAFAFC"), CardBg: Hex("#FFFFFF"), CardBorder: Hex("#E5E5EB"),
            SidebarBg: Hex("#F5F5F8"), InputBg: Hex("#F5F5F8"), SubtleBg: Hex("#EEEEF2"),
            Text: Hex("#0E0E12"), Muted: Hex("#6B6B85"), CheckBorder: Hex("#D0D0DA"),
            HoverBg: Hex("#EAEAF0"), ProgressTrack: Hex("#E8E8EE"), KeyboardBg: Hex("#EEEEF2"),
            Success: Hex("#10B981"), Warning: Hex("#D97706"), Danger: Hex("#DC2626"), Info: Hex("#2563EB")),
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
