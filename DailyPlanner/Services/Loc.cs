using System.Collections.Frozen;
using System.ComponentModel;
using System.IO;

namespace DailyPlanner.Services;

/// <summary>
/// Localization service — singleton with indexer for XAML bindings.
/// Usage in XAML:  {Binding [Key], Source={x:Static services:Loc.Instance}}
/// Usage in code:  Loc.Get("Key")
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _lang = "ru";
    public string Language
    {
        get => _lang;
        set
        {
            if (_lang == value) return;
            _lang = value;
            Save();
            // Notify all bindings via indexer
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            LanguageChanged?.Invoke();
        }
    }

    public static event Action? LanguageChanged;

    /// <summary>
    /// CultureInfo matching the app's ACTIVE UI LANGUAGE, not the OS culture.
    /// Use for any user-visible date/number formatting — CurrentCulture would mix
    /// OS-language month/day names into an app running in another language.
    /// </summary>
    public System.Globalization.CultureInfo Culture
    {
        get
        {
            try { return System.Globalization.CultureInfo.GetCultureInfo(_lang); }
            catch (System.Globalization.CultureNotFoundException) { return System.Globalization.CultureInfo.CurrentCulture; }
        }
    }

    public string this[string key] => Get(key);

    public static string Get(string key)
    {
        var lang = Instance._lang;
        if (Translations.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        // Fallback to Russian
        if (Translations.TryGetValue("ru", out var ruDict) && ruDict.TryGetValue(key, out var ruVal))
            return ruVal;
        Log.Error("Loc", $"Missing key: {key}");
        return key;
    }

    // Bypass active language — used when migrating data seeded under any language.
    public static string GetIn(string lang, string key) =>
        Translations.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var val) ? val : key;

    public static readonly FrozenSet<string> SupportedLanguages =
        new[] { "ru", "en", "es", "fr" }.ToFrozenSet(StringComparer.Ordinal);
    public static readonly FrozenDictionary<string, string> LanguageNames =
        new Dictionary<string, string>
        {
            ["ru"] = "Русский",
            ["en"] = "English",
            ["es"] = "Español",
            ["fr"] = "Français"
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DailyPlanner", "language.txt");

    public Loc()
    {
        _lang = LoadSaved();
    }

    private static string LoadSaved()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var lang = File.ReadAllText(SettingsPath).Trim();
                if (SupportedLanguages.Contains(lang)) return lang;
            }
        }
        catch (Exception ex) { Log.Error("Loc", $"LoadSaved failed: {ex.Message}"); }
        return "ru";
    }

    private static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath, Instance._lang);
        }
        catch (Exception ex) { Log.Error("Loc", $"Save failed: {ex.Message}"); }
    }

    // ─── All translations ────────────────────────────────────────────
    // Frozen at startup — read-mostly lookup (every XAML binding hits this).
    private static readonly FrozenDictionary<string, FrozenDictionary<string, string>> Translations =
        new Dictionary<string, FrozenDictionary<string, string>>
        {
            ["ru"] = LocRuStrings.Build().ToFrozenDictionary(StringComparer.Ordinal),
            ["en"] = LocEnStrings.Build().ToFrozenDictionary(StringComparer.Ordinal),
            ["es"] = LocEsStrings.Build().ToFrozenDictionary(StringComparer.Ordinal),
            ["fr"] = LocFrStrings.Build().ToFrozenDictionary(StringComparer.Ordinal),
        }.ToFrozenDictionary(StringComparer.Ordinal);

    // Helper: get month name for current language
    public static string GetMonthName(int month) => Get($"Month{month}");

    // Helper: get day name for current language
    public static string GetDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Get("Monday"),
        DayOfWeek.Tuesday => Get("Tuesday"),
        DayOfWeek.Wednesday => Get("Wednesday"),
        DayOfWeek.Thursday => Get("Thursday"),
        DayOfWeek.Friday => Get("Friday"),
        DayOfWeek.Saturday => Get("Saturday"),
        DayOfWeek.Sunday => Get("Sunday"),
        _ => ""
    };

    public static string GetShortDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Get("Mon"),
        DayOfWeek.Tuesday => Get("Tue"),
        DayOfWeek.Wednesday => Get("Wed"),
        DayOfWeek.Thursday => Get("Thu"),
        DayOfWeek.Friday => Get("Fri"),
        DayOfWeek.Saturday => Get("Sat"),
        DayOfWeek.Sunday => Get("Sun"),
        _ => ""
    };
}
