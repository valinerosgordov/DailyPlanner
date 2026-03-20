using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class DebtViewModel : ObservableObject
{
    private readonly Debt _model;
    private readonly PlannerService _service;

    public DebtViewModel(Debt model, PlannerService service)
    {
        _model = model;
        _service = service;
        _personName = model.PersonName;
        _amount = model.Amount;
        _direction = model.Direction;
        _description = model.Description;
        _dueDate = model.DueDate;
        _isSettled = model.IsSettled;

        foreach (var p in model.Payments.OrderByDescending(p => p.Date))
            Payments.Add(new DebtPaymentViewModel(p, service, this));
    }

    public Debt Model => _model;
    public ObservableCollection<DebtPaymentViewModel> Payments { get; } = [];

    [ObservableProperty] private string _personName;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private DebtDirection _direction;
    [ObservableProperty] private string _description;
    [ObservableProperty] private DateOnly? _dueDate;
    [ObservableProperty] private bool _isSettled;

    public decimal PaidAmount => _model.Payments.Sum(p => p.Amount);
    public decimal RemainingAmount => Amount - PaidAmount;
    public double ProgressPercent => Amount > 0 ? Math.Min((double)PaidAmount / (double)Amount * 100, 100) : 0;
    public string DisplayCreatedDate => _model.CreatedDate.ToString("dd.MM.yyyy");

    public bool IsOverdue => !IsSettled && DueDate is not null
        && DueDate < DateOnly.FromDateTime(DateTime.Today);

    public string DirectionLabel => Direction == DebtDirection.Lent
        ? Loc.Get("DebtLent")
        : Loc.Get("DebtBorrowed");

    partial void OnPersonNameChanged(string value) { _model.PersonName = value; Save(); }
    partial void OnAmountChanged(decimal value)
    {
        if (value < 0) { Amount = 0; return; }
        _model.Amount = value;
        RefreshComputed();
        Save();
    }
    partial void OnDescriptionChanged(string value) { _model.Description = value; Save(); }
    partial void OnDueDateChanged(DateOnly? value)
    {
        _model.DueDate = value;
        OnPropertyChanged(nameof(IsOverdue));
        Save();
    }
    partial void OnIsSettledChanged(bool value)
    {
        _model.IsSettled = value;
        _model.SettledDate = value ? DateOnly.FromDateTime(DateTime.Today) : null;
        OnPropertyChanged(nameof(IsOverdue));
        Save();
    }

    public void AddPayment(DebtPayment payment)
    {
        _model.Payments.Add(payment);
        Payments.Insert(0, new DebtPaymentViewModel(payment, _service, this));
        RefreshComputed();
    }

    public void RefreshComputed()
    {
        OnPropertyChanged(nameof(PaidAmount));
        OnPropertyChanged(nameof(RemainingAmount));
        OnPropertyChanged(nameof(ProgressPercent));
    }

    private void Save()
    {
        DebounceService.Debounce($"debt-{_model.Id}",
            () => _service.SaveDebtAsync(_model));
    }
}
