using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class FinanceCategoryViewModel : ObservableObject
{
    private readonly FinanceCategory _model;
    private readonly PlannerService _service;

    public FinanceCategoryViewModel(FinanceCategory model, PlannerService service)
    {
        _model = model;
        _service = service;
        _name = model.Name;
        _icon = model.Icon;
        _color = model.Color;
        _type = model.Type;
    }

    public FinanceCategory Model => _model;
    public int Id => _model.Id;

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _icon;
    [ObservableProperty] private string _color;
    [ObservableProperty] private FinanceEntryType _type;

    partial void OnNameChanged(string value) { _model.Name = value; Save(); }
    partial void OnIconChanged(string value) { _model.Icon = value; Save(); }
    partial void OnColorChanged(string value) { _model.Color = value; Save(); }

    private void Save()
    {
        DebounceService.Debounce($"finance-cat-{_model.Id}",
            () => _service.SaveFinanceCategoryAsync(_model));
    }
}
