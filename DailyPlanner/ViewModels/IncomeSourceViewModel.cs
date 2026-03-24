using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class IncomeSourceViewModel : ObservableObject
{
    private readonly IncomeSource _model;
    private readonly PlannerService _service;

    public IncomeSourceViewModel(IncomeSource model, PlannerService service)
    {
        _model = model;
        _service = service;
        _name = model.Name;
        _clientName = model.ClientName;
        _icon = model.Icon;
        _totalMonthlyAmount = model.TotalMonthlyAmount;
        _isActive = model.IsActive;
        _note = model.Note;

        foreach (var p in model.Payments.OrderBy(p => p.DayOfMonth))
            Payments.Add(new IncomeSourcePaymentViewModel(p, service));
    }

    public IncomeSource Model => _model;

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _clientName;
    [ObservableProperty] private string _icon;
    [ObservableProperty] private decimal _totalMonthlyAmount;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private string _note;

    public ObservableCollection<IncomeSourcePaymentViewModel> Payments { get; } = [];

    public bool HasPayments => Payments.Count > 0;
    public decimal ScheduledTotal => Payments.Sum(p => p.Amount);
    public string DisplayTotal => $"+{TotalMonthlyAmount:N2}";
    public int PaymentCount => Payments.Count;

    [RelayCommand]
    private async Task AddPaymentAsync()
    {
        var payment = new IncomeSourcePayment
        {
            IncomeSourceId = _model.Id,
            DayOfMonth = 1
        };
        await _service.SaveIncomeSourcePaymentAsync(payment);
        _model.Payments.Add(payment);
        Payments.Add(new IncomeSourcePaymentViewModel(payment, _service));
        OnPropertyChanged(nameof(HasPayments));
        OnPropertyChanged(nameof(PaymentCount));
        OnPropertyChanged(nameof(ScheduledTotal));
    }

    [RelayCommand]
    private async Task RemovePaymentAsync(IncomeSourcePaymentViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveIncomeSourcePaymentAsync(vm.Model.Id);
        _model.Payments.Remove(vm.Model);
        Payments.Remove(vm);
        OnPropertyChanged(nameof(HasPayments));
        OnPropertyChanged(nameof(PaymentCount));
        OnPropertyChanged(nameof(ScheduledTotal));
    }

    partial void OnNameChanged(string value) { _model.Name = value; Save(); }
    partial void OnClientNameChanged(string value) { _model.ClientName = value; Save(); }
    partial void OnIconChanged(string value) { _model.Icon = value; Save(); }
    partial void OnTotalMonthlyAmountChanged(decimal value)
    {
        if (value < 0) { TotalMonthlyAmount = 0; return; }
        _model.TotalMonthlyAmount = value;
        OnPropertyChanged(nameof(DisplayTotal));
        Save();
    }
    partial void OnIsActiveChanged(bool value) { _model.IsActive = value; Save(); }
    partial void OnNoteChanged(string value) { _model.Note = value; Save(); }

    private void Save()
    {
        DebounceService.Debounce($"incsrc-{_model.Id}",
            () => _service.SaveIncomeSourceAsync(_model));
    }
}
