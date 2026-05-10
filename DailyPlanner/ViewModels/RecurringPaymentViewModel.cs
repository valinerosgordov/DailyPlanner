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
        _isSubscription = model.IsSubscription;
        _currency = string.IsNullOrEmpty(model.Currency) ? "RUB" : model.Currency;
        _billingIntervalMonths = model.BillingIntervalMonths < 1 ? 1 : model.BillingIntervalMonths;
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
    [ObservableProperty] private bool _isSubscription;
    [ObservableProperty] private string _currency;
    [ObservableProperty] private int _billingIntervalMonths;

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

    /// <summary>
    /// Per-month cost normalized for commitment-window pricing. For monthly billing
    /// = Amount; for Timeweb-style yearly = Amount/12; for a 6-month JetBrains
    /// commit = Amount/6. Use this for "monthly burden" aggregates so foreign
    /// commitment payments stop showing up as huge spikes in single months.
    /// </summary>
    public decimal EffectiveMonthlyCost
    {
        get
        {
            var months = BillingIntervalMonths < 1 ? 1 : BillingIntervalMonths;
            return System.Math.Round(Amount / months, 2);
        }
    }

    public string EffectiveMonthlyLabel => $"{EffectiveMonthlyCost:N2} {Currency}/мес";

    /// <summary>
    /// Computed status for the subscription badge (pending vs active vs overdue).
    /// Drives the colored chip on the right of each subscription row.
    /// </summary>
    public string StatusKey
    {
        get
        {
            if (!IsActive) return "Paused";
            if (!IsSubscription) return "Recurring";
            var today = DateOnly.FromDateTime(System.DateTime.Today);
            var next = SubscriptionForecastService.NextOccurrenceFrom(_model, today);
            if (next is null) return "Inactive";
            var days = next.Value.DayNumber - today.DayNumber;
            if (days < 0) return "Overdue";
            if (days == 0) return "DueToday";
            if (days <= 7) return "DueSoon";
            return "Pending";
        }
    }

    public string StatusLabel => StatusKey switch
    {
        "Paused" => Loc.Get("SubStatusPaused"),
        "Recurring" => Loc.Get("SubStatusRecurring"),
        "Overdue" => Loc.Get("SubStatusOverdue"),
        "DueToday" => Loc.Get("SubStatusDueToday"),
        "DueSoon" => Loc.Get("SubStatusDueSoon"),
        "Pending" => Loc.Get("SubStatusPending"),
        _ => string.Empty
    };

    public string NextPaymentDate
    {
        get
        {
            var today = DateOnly.FromDateTime(System.DateTime.Today);
            if (!IsActive) return string.Empty;
            var next = SubscriptionForecastService.NextOccurrenceFrom(_model, today);
            return next?.ToString("dd.MM.yyyy") ?? string.Empty;
        }
    }

    partial void OnNameChanged(string value) { _model.Name = value; Save(); }
    partial void OnAmountChanged(decimal value)
    {
        if (value < 0) { Amount = 0; return; }
        _model.Amount = value;
        OnPropertyChanged(nameof(DisplayAmount));
        OnPropertyChanged(nameof(AnnualTotal));
        OnPropertyChanged(nameof(EffectiveMonthlyCost));
        OnPropertyChanged(nameof(EffectiveMonthlyLabel));
        Save();
    }
    partial void OnFrequencyChanged(PaymentFrequency value)
    {
        _model.Frequency = value;
        OnPropertyChanged(nameof(FrequencyLabel));
        OnPropertyChanged(nameof(ScheduleLabel));
        OnPropertyChanged(nameof(NextPaymentDate));
        OnPropertyChanged(nameof(StatusKey));
        OnPropertyChanged(nameof(StatusLabel));
        Save();
    }
    partial void OnDayOfMonthChanged(int? value)
    {
        _model.DayOfMonth = value;
        OnPropertyChanged(nameof(ScheduleLabel));
        OnPropertyChanged(nameof(NextPaymentDate));
        Save();
    }
    partial void OnIsActiveChanged(bool value)
    {
        _model.IsActive = value;
        OnPropertyChanged(nameof(StatusKey));
        OnPropertyChanged(nameof(StatusLabel));
        Save();
    }
    partial void OnAutoCreateChanged(bool value) { _model.AutoCreate = value; Save(); }
    partial void OnNoteChanged(string value) { _model.Note = value; Save(); }
    partial void OnIsSubscriptionChanged(bool value)
    {
        _model.IsSubscription = value;
        OnPropertyChanged(nameof(StatusKey));
        OnPropertyChanged(nameof(StatusLabel));
        Save();
    }
    partial void OnCurrencyChanged(string value)
    {
        _model.Currency = string.IsNullOrEmpty(value) ? "RUB" : value;
        OnPropertyChanged(nameof(EffectiveMonthlyLabel));
        Save();
    }
    partial void OnBillingIntervalMonthsChanged(int value)
    {
        _model.BillingIntervalMonths = value < 1 ? 1 : value;
        OnPropertyChanged(nameof(EffectiveMonthlyCost));
        OnPropertyChanged(nameof(EffectiveMonthlyLabel));
        Save();
    }

    private void Save()
    {
        DebounceService.Debounce($"recurring-{_model.Id}",
            () => _service.SaveRecurringPaymentAsync(_model));
    }
}