using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class BudgetViewModel : ObservableObject
{
    private readonly FinanceBudget _model;
    private readonly PlannerService _service;

    public BudgetViewModel(FinanceBudget model, PlannerService service)
    {
        _model = model;
        _service = service;
        _amount = model.Amount;
    }

    public FinanceBudget Model => _model;
    public string CategoryName => _model.Category?.Name ?? string.Empty;
    public string CategoryIcon => _model.Category?.Icon ?? string.Empty;
    public string CategoryColor => _model.Category?.Color ?? string.Empty;

    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private decimal _spentAmount;

    public decimal RemainingAmount => Amount - SpentAmount;
    public double ProgressPercent => Amount > 0 ? Math.Min((double)SpentAmount / (double)Amount * 100, 100) : 0;

    public bool IsOverBudget => SpentAmount > Amount;
    public bool IsWarning => ProgressPercent >= 80 && !IsOverBudget;
    public string RemainingText => IsOverBudget
        ? $"{Loc.Get("OverBudget")}: {SpentAmount - Amount:N2}"
        : $"{Loc.Get("FinRemaining")}: {RemainingAmount:N2}";
    public bool ShowWarning => IsOverBudget || IsWarning;
    public string WarningText => IsOverBudget
        ? Loc.Get("OverBudget")
        : IsWarning ? $"{ProgressPercent:N0}% {Loc.Get("Spent").ToLowerInvariant()}" : string.Empty;

    partial void OnAmountChanged(decimal value)
    {
        if (value < 0) { Amount = 0; return; }
        _model.Amount = value;
        OnPropertyChanged(nameof(RemainingAmount));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(IsOverBudget));
        OnPropertyChanged(nameof(IsWarning));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(ShowWarning));
        OnPropertyChanged(nameof(WarningText));
        Save();
    }

    partial void OnSpentAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(RemainingAmount));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(IsOverBudget));
        OnPropertyChanged(nameof(IsWarning));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(ShowWarning));
        OnPropertyChanged(nameof(WarningText));
    }

    private void Save()
    {
        DebounceService.Debounce($"budget-{_model.Id}",
            () => _service.SaveBudgetAsync(_model));
    }
}
