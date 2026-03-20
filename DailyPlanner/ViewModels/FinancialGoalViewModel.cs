using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class FinancialGoalViewModel : ObservableObject
{
    private readonly FinancialGoal _model;
    private readonly PlannerService _service;

    public FinancialGoalViewModel(FinancialGoal model, PlannerService service)
    {
        _model = model;
        _service = service;
        _name = model.Name;
        _icon = model.Icon;
        _targetAmount = model.TargetAmount;
        _savedAmount = model.SavedAmount;
        _targetDate = model.TargetDate;
        _isCompleted = model.IsCompleted;
    }

    public FinancialGoal Model => _model;

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _icon;
    [ObservableProperty] private decimal _targetAmount;
    [ObservableProperty] private decimal _savedAmount;
    [ObservableProperty] private DateOnly? _targetDate;
    [ObservableProperty] private bool _isCompleted;

    public double ProgressPercent => TargetAmount > 0
        ? Math.Min(100, Math.Round((double)SavedAmount / (double)TargetAmount * 100, 1))
        : 0;

    public string ProgressText => string.Format(Loc.Get("GoalProgress"), ProgressPercent.ToString("N0"));
    public string DisplayTargetDate => TargetDate?.ToString("dd.MM.yyyy") ?? "";
    public decimal RemainingAmount => Math.Max(0, TargetAmount - SavedAmount);

    partial void OnNameChanged(string value) { _model.Name = value; Save(); }
    partial void OnIconChanged(string value) { _model.Icon = value; Save(); }
    partial void OnTargetAmountChanged(decimal value)
    {
        if (value < 0) { TargetAmount = 0; return; }
        _model.TargetAmount = value;
        RefreshComputed();
        Save();
    }
    partial void OnSavedAmountChanged(decimal value)
    {
        if (value < 0) { SavedAmount = 0; return; }
        _model.SavedAmount = value;
        if (value >= TargetAmount && TargetAmount > 0) IsCompleted = true;
        RefreshComputed();
        Save();
    }
    partial void OnTargetDateChanged(DateOnly? value)
    {
        _model.TargetDate = value;
        OnPropertyChanged(nameof(DisplayTargetDate));
        Save();
    }
    partial void OnIsCompletedChanged(bool value) { _model.IsCompleted = value; Save(); }

    private void RefreshComputed()
    {
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(RemainingAmount));
    }

    private void Save()
    {
        DebounceService.Debounce($"fin-goal-{_model.Id}",
            () => _service.SaveFinancialGoalAsync(_model));
    }
}
