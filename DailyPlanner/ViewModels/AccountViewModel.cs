using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class AccountViewModel : ObservableObject
{
    private readonly Account _model;
    private readonly PlannerService _service;

    public AccountViewModel(Account model, PlannerService service)
    {
        _model = model;
        _service = service;
        _name = model.Name;
        _icon = model.Icon;
        _color = model.Color;
        _initialBalance = model.InitialBalance;
    }

    public Account Model => _model;
    public int Id => _model.Id;

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _icon;
    [ObservableProperty] private string _color;
    [ObservableProperty] private decimal _initialBalance;
    [ObservableProperty] private decimal _currentBalance;

    partial void OnNameChanged(string value) { _model.Name = value; Save(); }
    partial void OnIconChanged(string value) { _model.Icon = value; Save(); }
    partial void OnColorChanged(string value) { _model.Color = value; Save(); }
    partial void OnInitialBalanceChanged(decimal value) { _model.InitialBalance = value; Save(); }

    private void Save()
    {
        DebounceService.Debounce($"account-{_model.Id}",
            () => _service.SaveAccountAsync(_model));
    }
}
