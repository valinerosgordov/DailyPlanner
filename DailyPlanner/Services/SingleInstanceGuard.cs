using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace DailyPlanner.Services;

public static partial class SingleInstanceGuard
{
    private const string MutexName = @"Global\DailyPlanner_Mutex_c3f4a2e1";
    private const string EventName = @"Global\DailyPlanner_Activate_c3f4a2e1";
    private const int SW_RESTORE = 9;

    private static Mutex? _mutex;
    private static EventWaitHandle? _activationEvent;
    private static EventWaitHandle? _shutdownEvent;
    private static Thread? _listenerThread;

    public static bool TryAcquire(out bool alreadyRunning)
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (createdNew)
        {
            _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
            _shutdownEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
            alreadyRunning = false;
            return true;
        }

        _mutex.Dispose();
        _mutex = null;
        alreadyRunning = true;
        return false;
    }

    public static void SignalExistingInstance()
    {
        try
        {
            using var signal = EventWaitHandle.OpenExisting(EventName);
            signal.Set();
        }
        catch
        {
            ActivateByWindowHandle();
        }
    }

    public static void StartActivationListener(Action onActivate)
    {
        if (_activationEvent is null || _shutdownEvent is null) return;

        var activation = _activationEvent;
        var shutdown = _shutdownEvent;
        var handles = new WaitHandle[] { activation, shutdown };

        _listenerThread = new Thread(() =>
        {
            while (true)
            {
                int idx;
                try { idx = WaitHandle.WaitAny(handles); }
                catch (ObjectDisposedException) { return; }
                if (idx != 0) return;
                Application.Current?.Dispatcher.BeginInvoke(onActivate);
            }
        })
        {
            IsBackground = true,
            Name = "DailyPlanner.ActivationListener"
        };
        _listenerThread.Start();
    }

    public static void Release()
    {
        _shutdownEvent?.Set();
        _listenerThread?.Join(TimeSpan.FromSeconds(1));
        _activationEvent?.Dispose();
        _shutdownEvent?.Dispose();
        try { _mutex?.ReleaseMutex(); } catch { }
        _mutex?.Dispose();
    }

    private static void ActivateByWindowHandle()
    {
        var current = Process.GetCurrentProcess();
        foreach (var p in Process.GetProcessesByName(current.ProcessName))
        {
            if (p.Id == current.Id) continue;
            var handle = p.MainWindowHandle;
            if (handle == nint.Zero) continue;
            if (IsIconic(handle)) ShowWindow(handle, SW_RESTORE);
            SetForegroundWindow(handle);
            return;
        }
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(nint hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(nint hWnd);
}
