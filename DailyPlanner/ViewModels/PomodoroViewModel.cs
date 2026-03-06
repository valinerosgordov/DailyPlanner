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
    [ObservableProperty] private string _phaseLabel = "Работа";
    [ObservableProperty] private int _sessionsCompleted;
    [ObservableProperty] private bool _isFocusMode;
    [ObservableProperty] private string _modeLabel = "Помодоро";
    [ObservableProperty] private double _workMinutes = 45;
    [ObservableProperty] private double _breakMinutes = 5;
    [ObservableProperty] private double _focusAlertMinutes = 45;

    public PomodoroViewModel()
    {
        _pomodoro.Tick += OnTick;
        _pomodoro.PhaseCompleted += OnPhaseCompleted;
        _pomodoro.FocusAlert += OnFocusAlert;
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
    }

    partial void OnBreakMinutesChanged(double value)
    {
        if (value < 1) { BreakMinutes = 1; return; }
        _pomodoro.BreakMinutes = (int)value;
    }

    partial void OnFocusAlertMinutesChanged(double value)
    {
        if (value < 1) { FocusAlertMinutes = 1; return; }
        _pomodoro.FocusAlertMinutes = (int)value;
    }

    private void OnTick()
    {
        TimeDisplay = _pomodoro.TimeDisplay;
        ProgressPercent = _pomodoro.ProgressPercent;
        IsRunning = _pomodoro.IsRunning;
        PhaseLabel = _pomodoro.IsFocusMode
            ? "Фокус"
            : (_pomodoro.IsWorkPhase ? "Работа" : "Перерыв");
    }

    private void OnPhaseCompleted(bool wasWork)
    {
        SessionsCompleted = _pomodoro.SessionsCompleted;
        var msg = wasWork ? "Время перерыва! Отдохни \U0001f3d6\ufe0f" : "Время работать! \U0001f4aa";
        NotificationService.ShowToast("Pomodoro", msg);
        System.Media.SystemSounds.Exclamation.Play();
    }

    private void OnFocusAlert()
    {
        var elapsed = _pomodoro.Elapsed;
        var mins = (int)elapsed.TotalMinutes;
        NotificationService.ShowToast("Фокус-таймер",
            $"Уже {mins} мин! Сделай перерыв \u2615");
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
        ModeLabel = "Помодоро";
        PhaseLabel = "Работа";
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
        ModeLabel = IsFocusMode ? "Фокус-режим" : "Помодоро";
        TimeDisplay = _pomodoro.TimeDisplay;
        ProgressPercent = 0;
        IsRunning = false;
        PhaseLabel = IsFocusMode ? "Фокус" : "Работа";
    }
}
