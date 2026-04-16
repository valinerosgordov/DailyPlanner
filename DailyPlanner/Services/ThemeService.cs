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

    public static readonly Dictionary<string, ThemePalette> Palettes = new()
    {
        // Modern Dark Minimal — violet accent, deep cool neutrals
        ["Dark"] = new("Dark", true,
            Accent: Hex("#7C3AED"), AccentLight: Hex("#A78BFA"), AccentDark: Hex("#6D28D9"),
            PageBg: Hex("#0E0E12"), CardBg: Hex("#17171F"), CardBorder: Hex("#242432"),
            SidebarBg: Hex("#0A0A0F"), InputBg: Hex("#1A1A24"), SubtleBg: Hex("#20202B"),
            Text: Hex("#F5F5FA"), Muted: Hex("#7E7E99"), CheckBorder: Hex("#343444"),
            HoverBg: Hex("#27273A"), ProgressTrack: Hex("#1A1A24"), KeyboardBg: Hex("#24242F"),
            Success: Hex("#10B981"), Warning: Hex("#F59E0B"), Danger: Hex("#EF4444"), Info: Hex("#3B82F6")),

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
