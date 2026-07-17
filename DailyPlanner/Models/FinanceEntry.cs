namespace DailyPlanner.Models;

public sealed class FinanceEntry
{
    public int Id { get; set; }
    public int? WeekId { get; set; }
    public DateOnly Date { get; set; }
    public int CategoryId { get; set; }
    public FinanceEntryType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public int? RecurringPaymentId { get; set; }
    public bool IsPaid { get; set; } = true;
    public int? AccountId { get; set; }
    public int? ParentEntryId { get; set; }

    /// <summary>
    /// Original currency the transaction happened in (ISO 4217, e.g. USD).
    /// Pre-currency rows default to RUB through migration backfill.
    /// </summary>
    public string Currency { get; set; } = "RUB";

    /// <summary>
    /// Exchange rate to the base currency (RUB) at the moment the entry was
    /// created. Stored so that historical totals stay stable even if today's
    /// rate moves. Null for base-currency entries (rate = 1).
    /// </summary>
    public decimal? ExchangeRateToBase { get; set; }

    /// <summary>
    /// Computed-and-stored Amount * ExchangeRateToBase. Stored (not pure
    /// computed) because we'd otherwise need a JOIN on every aggregate query.
    /// Updated whenever Amount, Currency, or ExchangeRateToBase change.
    /// </summary>
    public decimal AmountInBaseCurrency { get; set; }

    /// <summary>
    /// The value every cross-entry aggregate must sum: the stamped base-currency
    /// amount when present, otherwise the raw amount (legacy rows / missing rate).
    /// Summing raw <see cref="Amount"/> across currencies counts 20 USD as 20 RUB.
    /// Not mapped (expression-bodied, no setter).
    /// </summary>
    public decimal BaseAmount => AmountInBaseCurrency != 0m ? AmountInBaseCurrency : Amount;

    public PlannerWeek? Week { get; set; }
    public FinanceCategory? Category { get; set; }
    public RecurringPayment? RecurringPayment { get; set; }
    public Account? Account { get; set; }
    public FinanceEntry? ParentEntry { get; set; }
    public List<FinanceEntry> SplitEntries { get; set; } = [];
}