using System.Windows.Threading;

namespace DailyPlanner.Services;

public sealed class PomodoroService
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private TimeSpan _remaining;
    private TimeSpan _elapsed;
    private bool _isWork = true;
    private bool _isFocusMode;

    public int WorkMinutes { get; set; } = 45;
    public int BreakMinutes { get; set; } = 5;
    public int FocusAlertMinutes { get; set; } = 45;
    public int SessionsCompleted { get; private set; }
    public bool IsRunning => _timer.IsEnabled;
    public bool IsWorkPhase => _isWork;
    public bool IsFocusMode => _isFocusMode;
    public TimeSpan Remaining => _remaining;
    public TimeSpan Elapsed => _elapsed;

    public string TimeDisplay => _isFocusMode
        ? _elapsed.ToString(_elapsed.TotalHours >= 1 ? @"h\:mm\:ss" : @"mm\:ss")
        : _remaining.ToString(@"mm\:ss");

    public double ProgressPercent => _isFocusMode
        ? (FocusAlertMinutes > 0 ? Math.Min(_elapsed.TotalMinutes / FocusAlertMinutes * 100, 100) : 0)
        : (GetTotalSeconds() > 0 ? (1.0 - _remaining.TotalSeconds / GetTotalSeconds()) * 100 : 0);

    public event Action? Tick;
    public event Action<bool>? PhaseCompleted;
    public event Action? FocusAlert; // fires when focus time exceeds alert interval

    private int _lastAlertAt;

    public PomodoroService()
    {
        _remaining = TimeSpan.FromMinutes(WorkMinutes);
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        if (!_timer.IsEnabled)
            _timer.Start();
    }

    public void Pause() => _timer.Stop();

    public void Reset()
    {
        _timer.Stop();
        _isWork = true;
        _isFocusMode = false;
        _remaining = TimeSpan.FromMinutes(WorkMinutes);
        _elapsed = TimeSpan.Zero;
        _lastAlertAt = 0;
        Tick?.Invoke();
    }

    public void Skip()
    {
        _timer.Stop();
        if (_isFocusMode)
        {
            Reset();
            return;
        }
        SwitchPhase();
    }

    public void SetFocusMode(bool enabled)
    {
        _timer.Stop();
        _isFocusMode = enabled;
        _elapsed = TimeSpan.Zero;
        _lastAlertAt = 0;
        if (!enabled)
        {
            _isWork = true;
            _remaining = TimeSpan.FromMinutes(WorkMinutes);
        }
        Tick?.Invoke();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_isFocusMode)
        {
            _elapsed += TimeSpan.FromSeconds(1);
            var alertMinutes = (int)_elapsed.TotalMinutes;
            if (FocusAlertMinutes > 0 && alertMinutes > 0
                && alertMinutes % FocusAlertMinutes == 0
                && alertMinutes != _lastAlertAt)
            {
                _lastAlertAt = alertMinutes;
                FocusAlert?.Invoke();
            }
            Tick?.Invoke();
            return;
        }

        _remaining -= TimeSpan.FromSeconds(1);
        if (_remaining <= TimeSpan.Zero)
        {
            _timer.Stop();
            if (_isWork) SessionsCompleted++;
            PhaseCompleted?.Invoke(_isWork);
            SwitchPhase();
        }
        Tick?.Invoke();
    }

    private void SwitchPhase()
    {
        _isWork = !_isWork;
        _remaining = TimeSpan.FromMinutes(_isWork ? WorkMinutes : BreakMinutes);
        Tick?.Invoke();
    }

    private double GetTotalSeconds() => (_isWork ? WorkMinutes : BreakMinutes) * 60.0;
}
