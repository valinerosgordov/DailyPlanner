using System.Data.Common;
using System.Text.Json;
using DailyPlanner.Data;
using DailyPlanner.Models;
using DailyPlanner.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DailyPlanner.Tests;

public class ExchangeRateServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly InMemorySqliteDbFactory _dbFactory;
    private readonly ExchangeRateService _service;

    public ExchangeRateServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _dbFactory = new InMemorySqliteDbFactory(_connection);

        using (var ctx = _dbFactory.CreateDbContext())
        {
            ctx.Database.Migrate();
        }

        _service = new ExchangeRateService(_dbFactory);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Seed(string currency, DateOnly date, decimal rate)
    {
        using var db = _dbFactory.CreateDbContext();
        db.ExchangeRates.Add(new ExchangeRate
        {
            CurrencyCode = currency,
            BaseCurrency = ExchangeRateService.BaseCurrency,
            Date = date,
            Rate = rate,
            Source = "test"
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GetRateAsync_BaseCurrency_ReturnsOne()
    {
        var rate = await _service.GetRateAsync("RUB", new DateOnly(2026, 5, 10));
        rate.Should().Be(1m);
    }

    [Fact]
    public async Task GetRateAsync_KnownDate_ReturnsExactRate()
    {
        Seed("USD", new DateOnly(2026, 5, 10), 90.5m);
        var rate = await _service.GetRateAsync("USD", new DateOnly(2026, 5, 10));
        rate.Should().Be(90.5m);
    }

    [Fact]
    public async Task GetRateAsync_MissingForExactDate_FallsBackToMostRecentBefore()
    {
        // Saturday — CBR doesn't publish. Friday's rate should be used.
        Seed("USD", new DateOnly(2026, 5, 8), 90m);  // Friday
        var rate = await _service.GetRateAsync("USD", new DateOnly(2026, 5, 9));  // Saturday
        rate.Should().Be(90m);
    }

    [Fact]
    public async Task GetRateAsync_NoRateAtAll_ReturnsNull()
    {
        var rate = await _service.GetRateAsync("USD", new DateOnly(2026, 5, 10));
        rate.Should().BeNull();
    }

    [Fact]
    public async Task ConvertToBaseAsync_BaseCurrency_ReturnsAmountUnchanged()
    {
        var result = await _service.ConvertToBaseAsync(1000m, "RUB", new DateOnly(2026, 5, 10));
        result.Should().Be(1000m);
    }

    [Fact]
    public async Task ConvertToBaseAsync_AppliesRate()
    {
        Seed("USD", new DateOnly(2026, 5, 10), 90.5m);
        // 20 USD × 90.5 = 1810 RUB
        var result = await _service.ConvertToBaseAsync(20m, "USD", new DateOnly(2026, 5, 10));
        result.Should().Be(1810m);
    }

    [Fact]
    public async Task ConvertToBaseAsync_NoRate_ReturnsUnconverted()
    {
        // Defensive fallback: better wrong total than crash dashboard.
        var result = await _service.ConvertToBaseAsync(20m, "USD", new DateOnly(2026, 5, 10));
        result.Should().Be(20m);
    }

    [Fact]
    public void CbrDailyResponse_ParsesActualCbrSchema()
    {
        const string json = """
        {
          "Date": "2026-05-09T11:30:00+03:00",
          "Valute": {
            "USD": { "CharCode": "USD", "Nominal": 1, "Value": 74.2963, "Previous": 74.5 },
            "JPY": { "CharCode": "JPY", "Nominal": 100, "Value": 50.12, "Previous": 50.0 }
          }
        }
        """;

        var parsed = JsonSerializer.Deserialize<ExchangeRateService.CbrDailyResponse>(json);

        parsed.Should().NotBeNull();
        parsed!.Valute.Should().ContainKey("USD");
        parsed.Valute!["USD"].Value.Should().Be(74.2963m);
        parsed.Valute["USD"].Nominal.Should().Be(1);
        parsed.Valute["JPY"].Nominal.Should().Be(100); // CBR uses Nominal=100 for low-denomination currencies
    }

    /// <summary>
    /// Local IDbContextFactory: shares one SqliteConnection across all
    /// created contexts so all see the same in-memory DB. Same pattern as
    /// PlannerServiceTestFixture.
    /// </summary>
    private sealed class InMemorySqliteDbFactory : IDbContextFactory<PlannerDbContext>
    {
        private readonly DbConnection _connection;
        public InMemorySqliteDbFactory(DbConnection connection) { _connection = connection; }
        public PlannerDbContext CreateDbContext()
        {
            var opts = new DbContextOptionsBuilder<PlannerDbContext>().UseSqlite(_connection).Options;
            return new PlannerDbContext(opts);
        }
    }
}