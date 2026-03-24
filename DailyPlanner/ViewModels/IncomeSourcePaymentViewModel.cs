using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class IncomeSourcePaymentViewModel : ObservableObject
{
    private readonly IncomeSourcePayment _model;
    private readonly PlannerService _service;

    public IncomeSourcePaymentViewModel(IncomeSourcePayment model, PlannerService service)
    {
        _model = model;
        _service = service;
        _dayOfMonth = model.DayOfMonth;
        _amount = model.Amount;
        _description = model.Description;
    }

    public IncomeSourcePayment Model => _model;

    [ObservableProperty] private int _dayOfMonth;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _description;

    public string DisplayDay => $"{DayOfMonth:D2}";

    partial void OnDayOfMonthChanged(int value)
    {
        if (value < 1) { DayOfMonth = 1; return; }
        if (value > 31) { DayOfMonth = 31; return; }
        _model.DayOfMonth = value;
        OnPropertyChanged(nameof(DisplayDay));
        Save();
    }

    partial void OnAmountChanged(decimal value)
    {
        if (value < 0) { Amount = 0; return; }
        _model.Amount = value;
        Save();
    }

    partial void OnDescriptionChanged(string value) { _model.Description = value; Save(); }

    private void Save()
    {
        DebounceService.Debounce($"isp-{_model.Id}",
            () => _service.SaveIncomeSourcePaymentAsync(_model));
    }
}
