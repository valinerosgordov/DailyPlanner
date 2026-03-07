using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class PomodoroViewModel : ObservableObject
{
    private readonly PomodoroService _pomodoro = new();

    [ObservableProperty] private string _timeDisplay = "45:00";
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _phaseLabel = Loc.Get("PomWork");
    [ObservableProperty] private int _sessionsCompleted;
    [ObservableProperty] private bool _isFocusMode;
    [ObservableProperty] private string _modeLabel = Loc.Get("PomPomodoro");
    [ObservableProperty] private double _workMinutes = 45;
    [ObservableProperty] private double _breakMinutes = 5;
    [ObservableProperty] private double _focusAlertMinutes = 45;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DailyPlanner", "pomodoro.txt");

    public PomodoroViewModel()
    {
        LoadSettings();
        _pomodoro.WorkMinutes = (int)_workMinutes;
        _pomodoro.BreakMinutes = (int)_breakMinutes;
        _pomodoro.FocusAlertMinutes = (int)_focusAlertMinutes;
        _pomodoro.Reset();
        _timeDisplay = _pomodoro.TimeDisplay;

        _pomodoro.Tick += OnTick;
        _pomodoro.PhaseCompleted += OnPhaseCompleted;
        _pomodoro.FocusAlert += OnFocusAlert;
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var lines = File.ReadAllLines(SettingsPath);
            #pragma warning disable MVVMTK0034
            if (lines.Length >= 3)
            {
                if (double.TryParse(lines[0], out var w) && w >= 1) _workMinutes = w;
                if (double.TryParse(lines[1], out var b) && b >= 1) _breakMinutes = b;
                if (double.TryParse(lines[2], out var f) && f >= 1) _focusAlertMinutes = f;
            }
            #pragma warning restore MVVMTK0034
        }
        catch { /* fallback to defaults */ }
    }

    private void SaveSettings()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllLines(SettingsPath, [$"{WorkMinutes}", $"{BreakMinutes}", $"{FocusAlertMinutes}"]);
        }
        catch { /* non-critical */ }
    }

    partial void OnWorkMinutesChanged(double value)
    {
        if (value < 1) { WorkMinutes = 1; return; }
        _pomodoro.WorkMinutes = (int)value;
        if (!_pomodoro.IsRunning && _pomodoro.IsWorkPhase && !_pomodoro.IsFocusMode)
        {
            _pomodoro.Reset();
            TimeDisplay = _pomodoro.TimeDisplay;
        }
        SaveSettings();
    }

    partial void OnBreakMinutesChanged(double value)
    {
        if (value < 1) { BreakMinutes = 1; return; }
        _pomodoro.BreakMinutes = (int)value;
        SaveSettings();
    }

    partial void OnFocusAlertMinutesChanged(double value)
    {
        if (value < 1) { FocusAlertMinutes = 1; return; }
        _pomodoro.FocusAlertMinutes = (int)value;
        SaveSettings();
    }

    private void OnTick()
    {
        TimeDisplay = _pomodoro.TimeDisplay;
        ProgressPercent = _pomodoro.ProgressPercent;
        IsRunning = _pomodoro.IsRunning;
        PhaseLabel = _pomodoro.IsFocusMode
            ? Loc.Get("PomFocus")
            : (_pomodoro.IsWorkPhase ? Loc.Get("PomWork") : Loc.Get("PomBreak"));
    }

    private void OnPhaseCompleted(bool wasWork)
    {
        SessionsCompleted = _pomodoro.SessionsCompleted;
        var msg = wasWork ? Loc.Get("PomBreakTime") : Loc.Get("PomWorkTime");
        NotificationService.ShowToast("Pomodoro", msg);
        System.Media.SystemSounds.Exclamation.Play();
    }

    private void OnFocusAlert()
    {
        var elapsed = _pomodoro.Elapsed;
        var mins = (int)elapsed.TotalMinutes;
        NotificationService.ShowToast(Loc.Get("PomFocusTimer"),
            string.Format(Loc.Get("PomFocusAlert"), mins));
        System.Media.SystemSounds.Exclamation.Play();
    }

    [RelayCommand]
    private void ToggleTimer()
    {
        if (_pomodoro.IsRunning)
            _pomodoro.Pause();
        else
            _pomodoro.Start();
        IsRunning = _pomodoro.IsRunning;
    }

    [RelayCommand]
    private void ResetTimer()
    {
        _pomodoro.Reset();
        TimeDisplay = _pomodoro.TimeDisplay;
        ProgressPercent = 0;
        IsRunning = false;
        IsFocusMode = false;
        ModeLabel = Loc.Get("PomPomodoro");
        PhaseLabel = Loc.Get("PomWork");
    }

    [RelayCommand]
    private void SkipPhase()
    {
        _pomodoro.Skip();
        OnTick();
    }

    [RelayCommand]
    private void ToggleFocusMode()
    {
        IsFocusMode = !IsFocusMode;
        _pomodoro.SetFocusMode(IsFocusMode);
        ModeLabel = IsFocusMode ? Loc.Get("PomFocusMode") : Loc.Get("PomPomodoro");
        TimeDisplay = _pomodoro.TimeDisplay;
        ProgressPercent = 0;
        IsRunning = false;
        PhaseLabel = IsFocusMode ? Loc.Get("PomFocus") : Loc.Get("PomWork");
    }
}
