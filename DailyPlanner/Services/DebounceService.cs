using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Services;

public static class DebounceService
{
    private sealed class PendingSave(CancellationTokenSource cts, Func<Task> action)
    {
        public CancellationTokenSource Cts { get; } = cts;
        public Func<Task> Action { get; } = action;

        private int _claimed;

        // Exactly-once guard between the delayed runner and FlushAll: whoever
        // claims first gets to execute the action, the other side skips.
        public bool TryClaim() => Interlocked.Exchange(ref _claimed, 1) == 0;
    }

    private static readonly ConcurrentDictionary<string, PendingSave> _pending = new();

    public static void Debounce(string key, Func<Task> action, int delayMs = 300)
    {
        if (_pending.TryRemove(key, out var existing))
        {
            existing.Cts.Cancel();
            existing.Cts.Dispose();
        }

        var item = new PendingSave(new CancellationTokenSource(), action);
        _pending[key] = item;

        _ = ExecuteAsync(key, item, delayMs);
    }

    /// <summary>
    /// Runs every pending action immediately instead of waiting out its delay.
    /// Called on app exit and right before a Velopack update restart — without
    /// this, edits made within the last debounce window are silently lost.
    /// </summary>
    public static void FlushAll(TimeSpan? timeout = null)
    {
        var tasks = new List<Task>();
        foreach (var kvp in _pending.ToArray())
        {
            if (!_pending.TryRemove(kvp)) continue;
            var item = kvp.Value;
            item.Cts.Cancel();
            if (item.TryClaim())
            {
                var key = kvp.Key;
                // Task.Run drops the UI SynchronizationContext so the WaitAll
                // below can't deadlock on continuations posting back here.
                tasks.Add(Task.Run(() => RunActionAsync(key, item.Action)));
            }
            item.Cts.Dispose();
        }
        if (tasks.Count == 0) return;

        try
        {
            Task.WaitAll([.. tasks], timeout ?? TimeSpan.FromSeconds(3));
        }
        catch (AggregateException ex)
        {
            Log.Error("DebounceService", $"Flush failed: {ex.Flatten().InnerExceptions.FirstOrDefault()?.Message}");
        }
    }

    private static async Task ExecuteAsync(string key, PendingSave item, int delayMs)
    {
        try
        {
            await Task.Delay(delayMs, item.Cts.Token);
            if (item.TryClaim())
                await RunActionAsync(key, item.Action);
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { } // displaced and disposed before the delay started
        finally
        {
            // Remove only OUR entry. A plain TryRemove(key) here used to evict
            // the NEXT debounce's CTS after this one was displaced — the
            // successor then ran uncancellable (double save) and its CTS leaked.
            if (_pending.TryRemove(new KeyValuePair<string, PendingSave>(key, item)))
                item.Cts.Dispose();
        }
    }

    private static async Task RunActionAsync(string key, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        // Row was deleted or moved between debounce schedule and save —
        // expected when user spam-edits then deletes a task.
        catch (DbUpdateConcurrencyException) { }
        catch (Exception ex)
        {
            Log.Error("DebounceService", $"Error in '{key}': {ex.Message}");
        }
    }
}
