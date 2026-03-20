using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class FinanceEntryViewModel : ObservableObject
{
    private readonly FinanceEntry _model;
    private readonly PlannerService _service;

    public FinanceEntryViewModel(FinanceEntry model, PlannerService service)
    {
        _model = model;
        _service = service;
        _amount = model.Amount;
        _description = model.Description;
        _date = model.Date;
        _type = model.Type;
        _categoryId = model.CategoryId;
        _isPaid = model.IsPaid;

        // Load existing splits
        foreach (var split in model.SplitEntries)
            SplitEntries.Add(new FinanceEntryViewModel(split, service));

        SplitEntries.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSplits));
            OnPropertyChanged(nameof(SplitTotal));
        };
    }

    public FinanceEntry Model => _model;

    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _description;
    [ObservableProperty] private DateOnly _date;
    [ObservableProperty] private FinanceEntryType _type;
    [ObservableProperty] private int _categoryId;
    [ObservableProperty] private bool _isPaid;

    public string CategoryName => _model.Category?.Name ?? string.Empty;
    public string CategoryIcon => _model.Category?.Icon ?? string.Empty;
    public string CategoryColor => _model.Category?.Color ?? string.Empty;
    public bool IsRecurring => _model.IsRecurring;
    public bool IsSplit => _model.ParentEntryId is not null;
    public bool HasSplits => SplitEntries.Count > 0;

    public ObservableCollection<FinanceEntryViewModel> SplitEntries { get; } = [];

    public string DisplayAmount => Type == FinanceEntryType.Income
        ? $"+{Amount:N2}"
        : $"-{Amount:N2}";

    public string DisplayDate => Date.ToString("dd.MM");
    public decimal SplitTotal => SplitEntries.Sum(s => s.Amount);

    partial void OnAmountChanged(decimal value)
    {
        if (value < 0) { Amount = 0; return; }
        _model.Amount = value;
        OnPropertyChanged(nameof(DisplayAmount));
        Save();
    }
    partial void OnDescriptionChanged(string value) { _model.Description = value; Save(); }
    partial void OnDateChanged(DateOnly value) { _model.Date = value; OnPropertyChanged(nameof(DisplayDate)); Save(); }
    partial void OnTypeChanged(FinanceEntryType value) { _model.Type = value; OnPropertyChanged(nameof(DisplayAmount)); Save(); }
    partial void OnCategoryIdChanged(int value) { _model.CategoryId = value; Save(); }
    partial void OnIsPaidChanged(bool value) { _model.IsPaid = value; Save(); }

    private void Save()
    {
        DebounceService.Debounce($"finance-entry-{_model.Id}",
            () => _service.SaveFinanceEntryAsync(_model));
    }
}
