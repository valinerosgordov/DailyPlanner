namespace DailyPlanner.Models;

/// <summary>
/// Daily exchange rate snapshot, keyed by (CurrencyCode, BaseCurrency, Date).
/// Powers historically-stable currency conversions: when a FinanceEntry is
/// created, the day's rate is captured into <see cref="FinanceEntry.ExchangeRateToBase"/>
/// so the row's RUB value never drifts as today's rate changes.
///
/// Populated by a background ExchangeRateService that pulls from the CBR
/// API once daily (see ExchangeRateService — separate PR).
/// </summary>
public sealed class ExchangeRate
{
    public int Id { get; set; }

    /// <summary>ISO 4217 code of the foreign currency (e.g. USD, EUR).</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>ISO 4217 base currency (RUB by default).</summary>
    public string BaseCurrency { get; set; } = "RUB";

    /// <summary>Rate: 1 unit of CurrencyCode = Rate units of BaseCurrency.</summary>
    public decimal Rate { get; set; }

    /// <summary>Date the rate applies to.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Source identifier for audit (e.g. "cbr.ru", "manual").</summary>
    public string Source { get; set; } = string.Empty;
}