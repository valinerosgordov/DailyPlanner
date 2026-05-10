namespace DailyPlanner.Models;

public sealed class RecurringPayment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public FinanceEntryType Type { get; set; }
    public int CategoryId { get; set; }
    public PaymentFrequency Frequency { get; set; }
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoCreate { get; set; }
    public int RemindDaysBefore { get; set; }
    public string Note { get; set; } = string.Empty;

    // === Subscription-specific fields (added 2026-05) ===

    /// <summary>
    /// True for SaaS / streaming / hosting / discretionary recurring expenses
    /// you could cancel. False for utilities, rent, loans — obligatory bills
    /// that also recur but aren't "subscriptions" in the cancellable sense.
    /// Separates the two in subscription-burden aggregates.
    /// </summary>
    public bool IsSubscription { get; set; }

    /// <summary>
    /// Months between actual charges. Frequency captures the cadence; this
    /// captures the commitment window. For monthly billing = 1; for an annual
    /// hosting plan billed once a year = 12; for a 6-month JetBrains commit
    /// pack = 6. EffectiveMonthlyCost = Amount / BillingIntervalMonths.
    /// </summary>
    public int BillingIntervalMonths { get; set; } = 1;

    /// <summary>
    /// What the same service would cost if billed monthly (the "no discount"
    /// tier). Lets us compute annual savings from a longer commitment, and
    /// surface "you save X by paying yearly" hints.
    /// </summary>
    public decimal? ListPriceMonthly { get; set; }

    /// <summary>
    /// When the entire commitment period renews (or auto-renews). For a yearly
    /// Timeweb plan paid in March 2026, this is March 2027. Drives the
    /// renewal-reminder ladder.
    /// </summary>
    public DateOnly? NextRenewalDate { get; set; }

    /// <summary>
    /// True if the provider auto-renews unless cancelled. For these, missing
    /// the cancellation deadline means an unwanted charge — reminders matter
    /// more. False for prepaid manual renewals.
    /// </summary>
    public bool AutoRenew { get; set; } = true;

    /// <summary>
    /// Some contracts require cancelling N days before renewal. JetBrains
    /// pack is "anytime", Timeweb is 0, some hosting/insurance products are
    /// 30+. CancellationDeadline = NextRenewalDate - CancellationNoticeDays.
    /// </summary>
    public int CancellationNoticeDays { get; set; }

    /// <summary>
    /// Comma-separated days-before-due for staggered reminders. Empty string
    /// = use legacy RemindDaysBefore (single-shot). Recommended for
    /// subscriptions: "30,7,1" — gentle nudge, action reminder, last call.
    /// </summary>
    public string RenewalRemindDaysBefore { get; set; } = string.Empty;

    /// <summary>
    /// ISO 4217 code (RUB, USD, EUR, …). Drives currency-aware aggregates so
    /// foreign-currency subscriptions don't get summed as if they were RUB.
    /// </summary>
    public string Currency { get; set; } = "RUB";

    /// <summary>
    /// Manually-set date when the user last reviewed this subscription
    /// (kept / cancelled / switched tier). Powers the "audit older than N
    /// days" prompt so users get nudged to revisit dormant subscriptions.
    /// </summary>
    public DateOnly? LastReviewedDate { get; set; }

    /// <summary>
    /// Optional trial-period end. When set, displayed prominently with a
    /// dedicated "trial ends in N days" reminder so the user can decide
    /// whether to keep the subscription before the first real charge.
    /// </summary>
    public DateOnly? TrialEndsOn { get; set; }

    public FinanceCategory? Category { get; set; }
    public List<FinanceEntry> GeneratedEntries { get; set; } = [];
}