using System.Collections.Concurrent;
using System.Diagnostics;

namespace DailyPlanner.Services;

public static class DebounceService
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _pending = new();

    public static void Debounce(string key, Func<Task> action, int delayMs = 300)
    {
        if (_pending.TryRemove(key, out var existing))
        {
            existing.Cancel();
            existing.Dispose();
        }

        var cts = new CancellationTokenSource();
        _pending[key] = cts;

        _ = ExecuteAsync(key, action, delayMs, cts.Token);
    }

    private static async Task ExecuteAsync(string key, Func<Task> action, int delayMs, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delayMs, ct);
            await action();
        }
        catch (TaskCanceledException)
        {
            // Debounced — expected
        }
        catch (Exception ex)
        {
            Log.Error("DebounceService", $"Error in '{key}': {ex.Message}");
        }
        finally
        {
            _pending.TryRemove(key, out _);
        }
    }
}
