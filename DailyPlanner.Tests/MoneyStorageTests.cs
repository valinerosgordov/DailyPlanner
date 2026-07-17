using DailyPlanner.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Tests;

/// <summary>
/// Pins the money-storage contract: decimals live in SQLite as exact TEXT
/// (HasColumnType("decimal(...)") used to fall into NUMERIC affinity and store
/// IEEE floats), and every entry carries a base-currency value so aggregates
/// never sum mixed currencies.
/// </summary>
public class MoneyStorageTests : PlannerServiceTestFixture
{
    private async Task<int> SeedExpenseCategoryAsync()
    {
        await Service.SeedFinanceCategoriesAsync();
        var categories = await Service.GetFinanceCategoriesAsync();
        return categories.First(c => c.Type == FinanceEntryType.Expense).Id;
    }

    [Fact]
    public async Task Amount_IsStoredAsExactText_NotFloat()
    {
        var catId = await SeedExpenseCategoryAsync();

        await Service.SaveFinanceEntryAsync(new FinanceEntry
        {
            Date = new DateOnly(2026, 7, 1),
            Type = FinanceEntryType.Expense,
            Amount = 1234.56m,
            CategoryId = catId
        });

        // Storage-level assert: the regression this guards against stored 'real'
        ExecuteScalar("SELECT typeof(Amount) FROM FinanceEntries LIMIT 1")!
            .ToString().Should().Be("text");

        var entries = await Service.GetFinanceEntriesAsync(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 1));
        entries.Single().Amount.Should().Be(1234.56m);
    }

    [Fact]
    public async Task SaveFinanceEntry_BaseCurrencyEntry_BaseEqualsAmount()
    {
        var catId = await SeedExpenseCategoryAsync();

        var entry = new FinanceEntry
        {
            Date = new DateOnly(2026, 7, 2),
            Type = FinanceEntryType.Expense,
            Amount = 500m,
            Currency = "RUB",
            CategoryId = catId
        };
        await Service.SaveFinanceEntryAsync(entry);

        entry.AmountInBaseCurrency.Should().Be(500m);
        entry.ExchangeRateToBase.Should().BeNull();
    }

    [Fact]
    public async Task SaveFinanceEntry_ForeignCurrency_StampsRateAndBaseAmount()
    {
        var catId = await SeedExpenseCategoryAsync();

        await using (var db = CreateContext())
        {
            db.ExchangeRates.Add(new ExchangeRate
            {
                CurrencyCode = "USD",
                BaseCurrency = "RUB",
                Date = new DateOnly(2026, 7, 1),
                Rate = 90.5m,
                Source = "test"
            });
            await db.SaveChangesAsync();
        }

        var entry = new FinanceEntry
        {
            Date = new DateOnly(2026, 7, 3),
            Type = FinanceEntryType.Expense,
            Amount = 20m,
            Currency = "USD",
            CategoryId = catId
        };
        await Service.SaveFinanceEntryAsync(entry);

        entry.ExchangeRateToBase.Should().Be(90.5m);
        entry.AmountInBaseCurrency.Should().Be(1810m, "20 USD at 90.5 must not be counted as 20 RUB");
    }

    [Fact]
    public async Task MonthlyTotals_SumBaseCurrency_NotRawMixedAmounts()
    {
        var catId = await SeedExpenseCategoryAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        await using (var db = CreateContext())
        {
            db.ExchangeRates.Add(new ExchangeRate
            {
                CurrencyCode = "USD",
                BaseCurrency = "RUB",
                Date = today.AddDays(-1),
                Rate = 100m,
                Source = "test"
            });
            await db.SaveChangesAsync();
        }

        await Service.SaveFinanceEntryAsync(new FinanceEntry
        {
            Date = today,
            Type = FinanceEntryType.Expense,
            Amount = 300m,
            Currency = "RUB",
            CategoryId = catId
        });
        await Service.SaveFinanceEntryAsync(new FinanceEntry
        {
            Date = today,
            Type = FinanceEntryType.Expense,
            Amount = 20m,
            Currency = "USD",
            CategoryId = catId
        });

        var totals = await Service.GetMonthlyTotalsAsync(1);
        var thisMonth = totals.Single(t => t.Year == today.Year && t.Month == today.Month);
        thisMonth.Expenses.Should().Be(2300m, "300 RUB + 20 USD * 100 = 2300 RUB, not 320");
    }

    [Fact]
    public async Task InboxExternalId_UniqueIndex_RejectsDuplicateCard()
    {
        await using var db = CreateContext();
        db.InboxTasks.Add(new InboxTask
        {
            Text = "first",
            Source = InboxSource.Trello,
            ExternalId = "dup-1",
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        });
        await db.SaveChangesAsync();

        db.InboxTasks.Add(new InboxTask
        {
            Text = "second",
            Source = InboxSource.Trello,
            ExternalId = "dup-1",
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>(
            "the schema, not just app code, must forbid duplicate Trello cards");
    }
}
