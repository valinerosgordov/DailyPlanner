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
                // Migrate old Russian palette name
                if (name == "\u0421\u0432\u0435\u0442\u043b\u0430\u044f") name = "Light";
                if (Palettes.ContainsKey(name)) return name;
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ThemeService] Load failed: {ex.Message}"); }
        return "Catppuccin Mocha";
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
        ["Catppuccin Mocha"] = new("Catppuccin Mocha", true,
            Accent: Hex("#cba6f7"), AccentLight: Hex("#b4befe"), AccentDark: Hex("#9399b2"),
            PageBg: Hex("#1e1e2e"), CardBg: Hex("#313244"), CardBorder: Hex("#45475a"),
            SidebarBg: Hex("#11111b"), InputBg: Hex("#1e1e2e"), SubtleBg: Hex("#45475a"),
            Text: Hex("#cdd6f4"), Muted: Hex("#6c7086"), CheckBorder: Hex("#585b70"),
            HoverBg: Hex("#585b70"), ProgressTrack: Hex("#1e1e2e"), KeyboardBg: Hex("#45475a"),
            Success: Hex("#a6e3a1"), Warning: Hex("#f9e2af"), Danger: Hex("#f38ba8"), Info: Hex("#89dceb")),

        ["Catppuccin Frappe"] = new("Catppuccin Frappe", true,
            Accent: Hex("#ca9ee6"), AccentLight: Hex("#babbf1"), AccentDark: Hex("#838ba7"),
            PageBg: Hex("#303446"), CardBg: Hex("#414559"), CardBorder: Hex("#51576d"),
            SidebarBg: Hex("#232634"), InputBg: Hex("#303446"), SubtleBg: Hex("#51576d"),
            Text: Hex("#c6d0f5"), Muted: Hex("#737994"), CheckBorder: Hex("#626880"),
            HoverBg: Hex("#626880"), ProgressTrack: Hex("#303446"), KeyboardBg: Hex("#51576d"),
            Success: Hex("#a6d189"), Warning: Hex("#e5c890"), Danger: Hex("#e78284"), Info: Hex("#99d1db")),

        ["Catppuccin Macchiato"] = new("Catppuccin Macchiato", true,
            Accent: Hex("#c6a0f6"), AccentLight: Hex("#b7bdf8"), AccentDark: Hex("#8087a2"),
            PageBg: Hex("#24273a"), CardBg: Hex("#363a4f"), CardBorder: Hex("#494d64"),
            SidebarBg: Hex("#181926"), InputBg: Hex("#24273a"), SubtleBg: Hex("#494d64"),
            Text: Hex("#cad3f5"), Muted: Hex("#6e738d"), CheckBorder: Hex("#5b6078"),
            HoverBg: Hex("#5b6078"), ProgressTrack: Hex("#24273a"), KeyboardBg: Hex("#494d64"),
            Success: Hex("#a6da95"), Warning: Hex("#eed49f"), Danger: Hex("#ed8796"), Info: Hex("#91d7e3")),

        ["Nord"] = new("Nord", true,
            Accent: Hex("#88c0d0"), AccentLight: Hex("#8fbcbb"), AccentDark: Hex("#5e81ac"),
            PageBg: Hex("#2e3440"), CardBg: Hex("#3b4252"), CardBorder: Hex("#434c5e"),
            SidebarBg: Hex("#272c36"), InputBg: Hex("#2e3440"), SubtleBg: Hex("#434c5e"),
            Text: Hex("#eceff4"), Muted: Hex("#7b88a1"), CheckBorder: Hex("#4c566a"),
            HoverBg: Hex("#4c566a"), ProgressTrack: Hex("#2e3440"), KeyboardBg: Hex("#434c5e"),
            Success: Hex("#a3be8c"), Warning: Hex("#ebcb8b"), Danger: Hex("#bf616a"), Info: Hex("#81a1c1")),

        ["Everforest"] = new("Everforest", true,
            Accent: Hex("#a7c080"), AccentLight: Hex("#83c092"), AccentDark: Hex("#7a8478"),
            PageBg: Hex("#272e33"), CardBg: Hex("#2e383c"), CardBorder: Hex("#3d484d"),
            SidebarBg: Hex("#1e2326"), InputBg: Hex("#272e33"), SubtleBg: Hex("#3d484d"),
            Text: Hex("#d3c6aa"), Muted: Hex("#7a8478"), CheckBorder: Hex("#56635f"),
            HoverBg: Hex("#56635f"), ProgressTrack: Hex("#272e33"), KeyboardBg: Hex("#3d484d"),
            Success: Hex("#a7c080"), Warning: Hex("#dbbc7f"), Danger: Hex("#e67e80"), Info: Hex("#7fbbb3")),

        ["Coffee"] = new("Coffee", true,
            Accent: Hex("#c4a882"), AccentLight: Hex("#d4b896"), AccentDark: Hex("#8b7355"),
            PageBg: Hex("#1c1610"), CardBg: Hex("#2a2018"), CardBorder: Hex("#3a2e22"),
            SidebarBg: Hex("#14100c"), InputBg: Hex("#1c1610"), SubtleBg: Hex("#3a2e22"),
            Text: Hex("#e8ddd0"), Muted: Hex("#8b7d6b"), CheckBorder: Hex("#5c4d3c"),
            HoverBg: Hex("#5c4d3c"), ProgressTrack: Hex("#1c1610"), KeyboardBg: Hex("#3a2e22"),
            Success: Hex("#a8b87c"), Warning: Hex("#d4a647"), Danger: Hex("#c75d5d"), Info: Hex("#7aacb5")),

        ["Graphite"] = new("Graphite", true,
            Accent: Hex("#8ca0b0"), AccentLight: Hex("#a0b4c4"), AccentDark: Hex("#607080"),
            PageBg: Hex("#1a1c20"), CardBg: Hex("#24272c"), CardBorder: Hex("#32363d"),
            SidebarBg: Hex("#131517"), InputBg: Hex("#1a1c20"), SubtleBg: Hex("#32363d"),
            Text: Hex("#d4d8de"), Muted: Hex("#6e7580"), CheckBorder: Hex("#4a5058"),
            HoverBg: Hex("#4a5058"), ProgressTrack: Hex("#1a1c20"), KeyboardBg: Hex("#32363d"),
            Success: Hex("#82b87c"), Warning: Hex("#c8b468"), Danger: Hex("#c07070"), Info: Hex("#70a0c0")),

        ["Obsidian"] = new("Obsidian", true,
            Accent: Hex("#c9a84c"), AccentLight: Hex("#d4b85c"), AccentDark: Hex("#8b6914"),
            PageBg: Hex("#0c0c0c"), CardBg: Hex("#151515"), CardBorder: Hex("#2a1a1a"),
            SidebarBg: Hex("#080808"), InputBg: Hex("#0c0c0c"), SubtleBg: Hex("#1e1212"),
            Text: Hex("#e8e0d4"), Muted: Hex("#6b5f55"), CheckBorder: Hex("#3d2828"),
            HoverBg: Hex("#3d2828"), ProgressTrack: Hex("#0c0c0c"), KeyboardBg: Hex("#1e1212"),
            Success: Hex("#c9a84c"), Warning: Hex("#d4a030"), Danger: Hex("#c43c3c"), Info: Hex("#a08060")),

        ["Light"] = new("Light", false,
            Accent: Hex("#7c5cfc"), AccentLight: Hex("#a78bfa"), AccentDark: Hex("#5b3fd6"),
            PageBg: Hex("#f5f6fa"), CardBg: Hex("#ffffff"), CardBorder: Hex("#e2e4eb"),
            SidebarBg: Hex("#eaecf3"), InputBg: Hex("#f0f1f6"), SubtleBg: Hex("#f0f1f6"),
            Text: Hex("#1a1c23"), Muted: Hex("#6b7085"), CheckBorder: Hex("#c8cad5"),
            HoverBg: Hex("#e4e5ec"), ProgressTrack: Hex("#e5e7ee"), KeyboardBg: Hex("#e8eaf0"),
            Success: Hex("#16a34a"), Warning: Hex("#ca8a04"), Danger: Hex("#dc2626"), Info: Hex("#0284c7")),
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
