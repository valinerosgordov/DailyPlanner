using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class AccountTransferViewModel : ObservableObject
{
    private readonly AccountTransfer _model;
    private readonly PlannerService _service;

    public AccountTransferViewModel(AccountTransfer model, PlannerService service)
    {
        _model = model;
        _service = service;
        _amount = model.Amount;
        _note = model.Note;
        _date = model.Date;
    }

    public AccountTransfer Model => _model;
    public string FromAccountName => _model.FromAccount?.Name ?? "";
    public string ToAccountName => _model.ToAccount?.Name ?? "";
    public string FromIcon => _model.FromAccount?.Icon ?? "💳";
    public string ToIcon => _model.ToAccount?.Icon ?? "💳";

    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _note;
    [ObservableProperty] private DateOnly _date;

    public string DisplayDate => Date.ToString("dd.MM");

    partial void OnAmountChanged(decimal value)
    {
        if (value < 0) { Amount = 0; return; }
        _model.Amount = value;
        Save();
    }
    partial void OnNoteChanged(string value) { _model.Note = value; Save(); }
    partial void OnDateChanged(DateOnly value) { _model.Date = value; OnPropertyChanged(nameof(DisplayDate)); Save(); }

    private void Save()
    {
        DebounceService.Debounce($"transfer-{_model.Id}",
            () => _service.SaveAccountTransferAsync(_model));
    }
}
