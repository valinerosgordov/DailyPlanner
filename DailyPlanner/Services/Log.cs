using System.IO;

namespace DailyPlanner.Services;

/// <summary>
/// Lightweight application logger. Writes to LocalAppData\DailyPlanner\app.log,
/// prefixing each entry with timestamp, level and category. Thread-safe.
/// </summary>
public static class Log
{
    public enum Level { Info, Warn, Error }

    private static readonly object _gate = new();
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DailyPlanner", "app.log");

    static Log()
    {
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            RotateIfLarge();
        }
        catch { /* logging should never throw */ }
    }

    private static void RotateIfLarge()
    {
        try
        {
            var info = new FileInfo(_logPath);
            if (info.Exists && info.Length > 2 * 1024 * 1024)
            {
                var backup = _logPath + ".old";
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(_logPath, backup);
            }
        }
        catch { }
    }

    public static void Info(string category, string message) => Write(Level.Info, category, message);
    public static void Warn(string category, string message) => Write(Level.Warn, category, message);
    public static void Error(string category, string message) => Write(Level.Error, category, message);
    public static void Error(string category, Exception ex) => Write(Level.Error, category, ex.ToString());

    private static void Write(Level level, string category, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{category}] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        try
        {
            lock (_gate)
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
        catch { }
    }
}
