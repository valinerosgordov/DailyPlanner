using System.Diagnostics;

namespace DailyPlanner.Services;

public static class TaskExtensions
{
    public static void FireAndForget(this Task task, string? context = null)
    {
        task.ContinueWith(t =>
        {
            if (t.Exception is not null)
                Debug.WriteLine($"[FireAndForget] {context}: {t.Exception}");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
