using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class DebtPaymentViewModel : ObservableObject
{
    private readonly DebtPayment _model;
    private readonly PlannerService? _service;
    private readonly DebtViewModel? _parent;

    public DebtPaymentViewModel(DebtPayment model, PlannerService? service = null, DebtViewModel? parent = null)
    {
        _model = model;
        _service = service;
        _parent = parent;
        _amount = model.Amount;
        _note = model.Note;
    }

    public DebtPayment Model => _model;
    public DateOnly Date => _model.Date;
    public string DisplayDate => _model.Date.ToString("dd.MM.yyyy");

    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _note;

    partial void OnAmountChanged(decimal value)
    {
        if (value < 0) { Amount = 0; return; }
        _model.Amount = value;
        _parent?.RefreshComputed();
        Save();
    }

    partial void OnNoteChanged(string value)
    {
        _model.Note = value;
        Save();
    }

    private void Save()
    {
        if (_service is null || _parent is null) return;
        DebounceService.Debounce($"debt-payment-{_model.Id}",
            () => _service.SaveDebtAsync(_parent.Model));
    }
}
