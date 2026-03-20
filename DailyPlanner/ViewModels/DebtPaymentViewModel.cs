using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;

namespace DailyPlanner.ViewModels;

public sealed partial class DebtPaymentViewModel : ObservableObject
{
    private readonly DebtPayment _model;

    public DebtPaymentViewModel(DebtPayment model)
    {
        _model = model;
    }

    public DebtPayment Model => _model;
    public decimal Amount => _model.Amount;
    public DateOnly Date => _model.Date;
    public string Note => _model.Note;
    public string DisplayDate => _model.Date.ToString("dd.MM.yyyy");
    public string DisplayAmount => $"{_model.Amount:N2}";
}
