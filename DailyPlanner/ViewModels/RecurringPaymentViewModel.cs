using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class RecurringPaymentViewModel : ObservableObject
{
    private readonly RecurringPayment _model;
    private readonly PlannerService _service;

    public RecurringPaymentViewModel(RecurringPayment model, PlannerService service)
    {
        _model = model;
        _service = service;
        _name = model.Name;
        _amount = model.Amount;
        _type = model.Type;
        _frequency = model.Frequency;
        _dayOfMonth = model.DayOfMonth;
        _isActive = model.IsActive;
        _autoCreate = model.AutoCreate;
        _note = model.Note;
    }

    public RecurringPayment Model => _model;
    public string CategoryName => _model.Category?.Name ?? string.Empty;
    public string CategoryIcon => _model.Category?.Icon ?? string.Empty;

    [ObservableProperty] private string _name;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private FinanceEntryType _type;
    [ObservableProperty] private PaymentFrequency _frequency;
    [ObservableProperty] private int? _dayOfMonth;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _autoCreate;
    [ObservableProperty] private string _note;

    public string DisplayAmount => Type == FinanceEntryType.Income
        ? $"+{Amount:N2}"
        : $"-{Amount:N2}";

    public string FrequencyLabel => Frequency switch
    {
        PaymentFrequency.Monthly => Loc.Get("FreqMonthly"),
        PaymentFrequency.Weekly => Loc.Get("FreqWeekly"),
        PaymentFrequency.Biweekly => Loc.Get("FreqBiweekly"),
        PaymentFrequency.Quarterly => Loc.Get("FreqQuarterly"),
        PaymentFrequency.Yearly => Loc.Get("FreqYearly"),
        _ => string.Empty
    };

    public string ScheduleLabel
    {
        get
        {
            if (DayOfMonth is not null)
                return $"{DayOfMonth}-{Loc.Get("DayOfMonthSuffix")}";
            if (_model.DayOfWeek is not null)
                return Loc.GetDayName(_model.DayOfWeek.Value);
            return string.Empty;
        }
    }

    public bool HasEndDate => _model.EndDate is not null;
    public string EndDateLabel => _model.EndDate?.ToString("dd.MM.yyyy") ?? string.Empty;

    public decimal AnnualTotal => Frequency switch
    {
        PaymentFrequency.Monthly => Amount * 12,
        PaymentFrequency.Weekly => Amount * 52,
        PaymentFrequency.Biweekly => Amount * 26,
        PaymentFrequency.Quarterly => Amount * 4,
        PaymentFrequency.Yearly => Amount,
        _ => 0
    };

    public string NextPaymentDate
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (!IsActive) return string.Empty;
            if (_model.EndDate is not null && _model.EndDate < today) return string.Empty;

            return Frequency switch
            {
                PaymentFrequency.Monthly when DayOfMonth is not null =>
                    (today.Day <= DayOfMonth
                        ? new DateOnly(today.Year, today.Month, Math.Min(DayOfMonth.Value, DateTime.DaysInMonth(today.Year, today.Month)))
                        : new DateOnly(today.Year, today.Month, 1).AddMonths(1)
                            .AddDays(Math.Min(DayOfMonth.Value, DateTime.DaysInMonth(today.AddMonths(1).Year, today.AddMonths(1).Month)) - 1))
                    .ToString("dd.MM.yyyy"),
                PaymentFrequency.Weekly when _model.DayOfWeek is not null =>
                    Enumerable.Range(0, 7).Select(i => today.AddDays(i))
                        .First(d => d.DayOfWeek == _model.DayOfWeek).ToString("dd.MM.yyyy"),
                _ => string.Empty
            };
        }
    }

    partial void OnNameChanged(string value) { _model.Name = value; Save(); }
    partial void OnAmountChanged(decimal value)
    {
        if (value < 0) { Amount = 0; return; }
        _model.Amount = value;
        OnPropertyChanged(nameof(DisplayAmount));
        OnPropertyChanged(nameof(AnnualTotal));
        Save();
    }
    partial void OnFrequencyChanged(PaymentFrequency value) { _model.Frequency = value; OnPropertyChanged(nameof(FrequencyLabel)); OnPropertyChanged(nameof(ScheduleLabel)); Save(); }
    partial void OnDayOfMonthChanged(int? value) { _model.DayOfMonth = value; OnPropertyChanged(nameof(ScheduleLabel)); Save(); }
    partial void OnIsActiveChanged(bool value) { _model.IsActive = value; Save(); }
    partial void OnAutoCreateChanged(bool value) { _model.AutoCreate = value; Save(); }
    partial void OnNoteChanged(string value) { _model.Note = value; Save(); }

    private void Save()
    {
        DebounceService.Debounce($"recurring-{_model.Id}",
            () => _service.SaveRecurringPaymentAsync(_model));
    }
}
