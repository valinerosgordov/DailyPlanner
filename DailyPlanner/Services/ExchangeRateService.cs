using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using DailyPlanner.Data;
using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Services;

/// <summary>
/// Currency conversion against a base currency (RUB). Rates are pulled
/// daily from the CBR public JSON feed and persisted into the ExchangeRates
/// table for historical stability — once a transaction captures a rate at
/// creation time, that rate is frozen on the entry itself
/// (<see cref="FinanceEntry.ExchangeRateToBase"/>) so the row's RUB value
/// never drifts as today's rate changes.
///
/// Source: https://www.cbr-xml-daily.ru/daily_json.js (no API key, free,
/// updated by the Central Bank around 11:30 MSK on banking days).
/// </summary>
public sealed class ExchangeRateService
{
    public const string BaseCurrency = "RUB";
    private const string CbrUrl = "https://www.cbr-xml-daily.ru/daily_json.js";

    private static readonly HttpClient Client = CreateClient();
    private readonly IDbContextFactory<PlannerDbContext> _dbFactory;

    public ExchangeRateService(IDbContextFactory<PlannerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private static HttpClient CreateClient()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };
        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"DailyPlanner/{version}");
        return client;
    }

    /// <summary>
    /// Returns the rate at which 1 unit of <paramref name="fromCurrency"/>
    /// converts to <see cref="BaseCurrency"/> on <paramref name="date"/>.
    /// Returns 1 for base-currency conversions. Returns null when no rate
    /// exists for that exact date — callers should fall back to the nearest
    /// available rate or skip the conversion.
    /// </summary>
    public async Task<decimal?> GetRateAsync(string fromCurrency, DateOnly date, CancellationToken ct = default)
    {
        if (string.Equals(fromCurrency, BaseCurrency, StringComparison.OrdinalIgnoreCase)) return 1m;

        await using var db = _dbFactory.CreateDbContext();

        // Exact date first; if missing, take the most recent rate up to that date.
        // This handles weekends/holidays (CBR doesn't publish, but FinanceEntry can
        // be backdated to a Saturday).
        var rate = await db.ExchangeRates
            .Where(r => r.CurrencyCode == fromCurrency
                && r.BaseCurrency == BaseCurrency
                && r.Date <= date)
            .OrderByDescending(r => r.Date)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        return rate;
    }

    /// <summary>
    /// Converts <paramref name="amount"/> from <paramref name="fromCurrency"/>
    /// to the base currency using the rate on <paramref name="date"/>.
    /// If no rate exists, returns the original amount unchanged (logged) —
    /// better to render a slightly wrong total than crash the dashboard.
    /// </summary>
    public async Task<decimal> ConvertToBaseAsync(
        decimal amount, string fromCurrency, DateOnly date, CancellationToken ct = default)
    {
        var rate = await GetRateAsync(fromCurrency, date, ct).ConfigureAwait(false);
        if (!rate.HasValue)
        {
            Log.Error("ExchangeRate",
                $"No rate found for {fromCurrency} on or before {date:yyyy-MM-dd}. " +
                $"Returning unconverted amount {amount}.");
            return amount;
        }
        return Math.Round(amount * rate.Value, 2);
    }

    /// <summary>
    /// Pulls the current rate snapshot from CBR and upserts each currency
    /// into the ExchangeRates table. Safe to call concurrently — uses the
    /// unique index (CurrencyCode, BaseCurrency, Date) to detect duplicates.
    /// </summary>
    public async Task<int> RefreshAsync(CancellationToken ct = default)
    {
        CbrDailyResponse? data;
        try
        {
            data = await Client.GetFromJsonAsync<CbrDailyResponse>(CbrUrl, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error("ExchangeRate", $"CBR fetch failed: {ex.Message}");
            return 0;
        }

        if (data is null || data.Valute is null || data.Valute.Count == 0)
        {
            Log.Error("ExchangeRate", "CBR response had no Valute data");
            return 0;
        }

        var date = DateOnly.FromDateTime(data.Date.UtcDateTime.Date);
        var upserted = 0;

        await using var db = _dbFactory.CreateDbContext();

        foreach (var (code, info) in data.Valute)
        {
            if (info.Nominal == 0) continue;
            var perUnit = Math.Round(info.Value / info.Nominal, 6);

            var existing = await db.ExchangeRates.FirstOrDefaultAsync(r =>
                r.CurrencyCode == code &&
                r.BaseCurrency == BaseCurrency &&
                r.Date == date, ct).ConfigureAwait(false);

            if (existing is null)
            {
                db.ExchangeRates.Add(new ExchangeRate
                {
                    CurrencyCode = code,
                    BaseCurrency = BaseCurrency,
                    Date = date,
                    Rate = perUnit,
                    Source = "cbr.ru"
                });
            }
            else if (existing.Rate != perUnit)
            {
                existing.Rate = perUnit;
            }
            upserted++;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        Log.Error("ExchangeRate", $"Refresh: {upserted} currencies upserted for {date:yyyy-MM-dd}");
        return upserted;
    }

    // === CBR API DTOs ===

    /// <summary>CBR daily JSON response top-level shape.</summary>
    public sealed class CbrDailyResponse
    {
        [JsonPropertyName("Date")]
        public DateTimeOffset Date { get; set; }

        [JsonPropertyName("Valute")]
        public Dictionary<string, CbrValute>? Valute { get; set; }
    }

    public sealed class CbrValute
    {
        [JsonPropertyName("CharCode")] public string CharCode { get; set; } = string.Empty;
        [JsonPropertyName("Nominal")] public int Nominal { get; set; } = 1;
        [JsonPropertyName("Value")] public decimal Value { get; set; }
        [JsonPropertyName("Previous")] public decimal Previous { get; set; }
    }
}