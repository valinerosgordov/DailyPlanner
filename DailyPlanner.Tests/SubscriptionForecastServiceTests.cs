using DailyPlanner.Models;
using DailyPlanner.Services;
using FluentAssertions;
using Xunit;

namespace DailyPlanner.Tests;

public class SubscriptionForecastServiceTests
{
    private static readonly DateOnly Today = new(2026, 5, 10);

    private static RecurringPayment NewSub(
        decimal amount = 590m,
        int billingMonths = 1,
        PaymentFrequency frequency = PaymentFrequency.Monthly,
        int? dayOfMonth = null,
        DateOnly? start = null,
        DateOnly? end = null,
        DateOnly? renewal = null,
        bool isActive = true,
        bool isSubscription = true,
        decimal? listPriceMonthly = null,
        string currency = "RUB",
        string remindCsv = "")
    {
        return new RecurringPayment
        {
            Name = "test",
            Amount = amount,
            Type = FinanceEntryType.Expense,
            Frequency = frequency,
            DayOfMonth = dayOfMonth,
            StartDate = start ?? new DateOnly(2026, 1, 1),
            EndDate = end,
            IsActive = isActive,
            IsSubscription = isSubscription,
            BillingIntervalMonths = billingMonths,
            ListPriceMonthly = listPriceMonthly,
            NextRenewalDate = renewal,
            Currency = currency,
            RenewalRemindDaysBefore = remindCsv
        };
    }

    // ── NextOccurrenceFrom ─────────────────────────────────────────────

    [Fact]
    public void NextOccurrenceFrom_Inactive_ReturnsNull()
    {
        var sub = NewSub(isActive: false);
        SubscriptionForecastService.NextOccurrenceFrom(sub, Today).Should().BeNull();
    }

    [Fact]
    public void NextOccurrenceFrom_EndedBeforeToday_ReturnsNull()
    {
        var sub = NewSub(end: new DateOnly(2026, 1, 1));
        SubscriptionForecastService.NextOccurrenceFrom(sub, Today).Should().BeNull();
    }

    [Fact]
    public void NextOccurrenceFrom_RespectsExplicitNextRenewalDate()
    {
        var sub = NewSub(renewal: new DateOnly(2027, 3, 15));
        SubscriptionForecastService.NextOccurrenceFrom(sub, Today)
            .Should().Be(new DateOnly(2027, 3, 15));
    }

    [Fact]
    public void NextOccurrenceFrom_MonthlyWithDayOfMonth_SnapsToCorrectDay()
    {
        // Started Jan 15. Today is May 10. Next monthly occurrence after today is May 15.
        var sub = NewSub(
            frequency: PaymentFrequency.Monthly,
            dayOfMonth: 15,
            start: new DateOnly(2026, 1, 15));

        SubscriptionForecastService.NextOccurrenceFrom(sub, Today)
            .Should().Be(new DateOnly(2026, 5, 15));
    }

    [Fact]
    public void NextOccurrenceFrom_YearlyBillingInterval_AdvancesYearly()
    {
        // Timeweb-style: paid March 2026, billingIntervalMonths=12, next March 2027.
        var sub = NewSub(
            billingMonths: 12,
            start: new DateOnly(2026, 3, 15));

        SubscriptionForecastService.NextOccurrenceFrom(sub, Today)
            .Should().Be(new DateOnly(2027, 3, 15));
    }

    // ── EnumerateOccurrences ───────────────────────────────────────────

    [Fact]
    public void EnumerateOccurrences_Monthly30DayWindow_YieldsOne()
    {
        var sub = NewSub(
            frequency: PaymentFrequency.Monthly,
            dayOfMonth: 20,
            start: new DateOnly(2026, 4, 20));

        var occs = SubscriptionForecastService.EnumerateOccurrences(sub, Today, window: 30).ToList();
        occs.Should().HaveCount(1);
        occs[0].Date.Should().Be(new DateOnly(2026, 5, 20));
    }

    [Fact]
    public void EnumerateOccurrences_Weekly60DayWindow_YieldsAboutEight()
    {
        var sub = NewSub(
            frequency: PaymentFrequency.Weekly,
            start: new DateOnly(2026, 5, 10));

        var occs = SubscriptionForecastService.EnumerateOccurrences(sub, Today, window: 60).ToList();
        occs.Should().HaveCountGreaterThanOrEqualTo(8);
        occs.Should().HaveCountLessThanOrEqualTo(9);
    }

    [Fact]
    public void EnumerateOccurrences_StopsAtEndDate()
    {
        var sub = NewSub(
            frequency: PaymentFrequency.Monthly,
            dayOfMonth: 15,
            start: new DateOnly(2026, 1, 15),
            end: new DateOnly(2026, 6, 30));

        var occs = SubscriptionForecastService.EnumerateOccurrences(sub, Today, window: 365).ToList();
        occs.Should().OnlyContain(o => o.Date <= new DateOnly(2026, 6, 30));
    }

    [Fact]
    public void EnumerateOccurrences_Inactive_YieldsEmpty()
    {
        var sub = NewSub(isActive: false);
        SubscriptionForecastService.EnumerateOccurrences(sub, Today, 30).Should().BeEmpty();
    }

    [Fact]
    public void EnumerateOccurrences_ClassifiesStatusCorrectly()
    {
        var sub = NewSub(
            frequency: PaymentFrequency.Monthly,
            dayOfMonth: 10,
            start: new DateOnly(2026, 5, 10));

        var occs = SubscriptionForecastService.EnumerateOccurrences(sub, Today, window: 30).ToList();
        occs[0].Date.Should().Be(Today);
        occs[0].Status.Should().Be(SubscriptionForecastService.OccurrenceStatus.DueToday);
    }

    // ── MonthlySubscriptionBurden ──────────────────────────────────────

    [Fact]
    public void MonthlySubscriptionBurden_SumsEffectiveCost()
    {
        // Spotify ₽590 monthly + Timeweb ₽10584 yearly (= ₽882/mo) + Netflix ₽800 monthly
        var subs = new[]
        {
            NewSub(amount: 590m, billingMonths: 1),
            NewSub(amount: 10584m, billingMonths: 12),
            NewSub(amount: 800m, billingMonths: 1)
        };

        SubscriptionForecastService.MonthlySubscriptionBurden(subs)
            .Should().Be(2272m); // 590 + 882 + 800
    }

    [Fact]
    public void MonthlySubscriptionBurden_ExcludesNonSubscriptionsAndInactive()
    {
        var subs = new[]
        {
            NewSub(amount: 100m, isSubscription: true),
            NewSub(amount: 200m, isSubscription: false),     // utility, not subscription
            NewSub(amount: 300m, isSubscription: true, isActive: false)  // paused
        };

        SubscriptionForecastService.MonthlySubscriptionBurden(subs).Should().Be(100m);
    }

    // ── AnnualSavingsVsMonthly ─────────────────────────────────────────

    [Fact]
    public void AnnualSavingsVsMonthly_CalculatesCorrectlyForTimewebCase()
    {
        // Pay ₽10584/yr = ₽882/mo. Monthly billing would be ₽980/mo.
        // Saving = (980 - 882) * 12 = ₽1176/yr
        var sub = NewSub(amount: 10584m, billingMonths: 12, listPriceMonthly: 980m);
        SubscriptionForecastService.AnnualSavingsVsMonthly([sub]).Should().Be(1176m);
    }

    [Fact]
    public void AnnualSavingsVsMonthly_SkipsWithoutListPriceMonthly()
    {
        var sub = NewSub(amount: 590m, listPriceMonthly: null);
        SubscriptionForecastService.AnnualSavingsVsMonthly([sub]).Should().Be(0m);
    }

    [Fact]
    public void AnnualSavingsVsMonthly_SkipsWhenNoDiscount()
    {
        // Same price monthly and yearly = no saving
        var sub = NewSub(amount: 1200m, billingMonths: 12, listPriceMonthly: 100m);
        SubscriptionForecastService.AnnualSavingsVsMonthly([sub]).Should().Be(0m);
    }

    // ── ParseRemindLadder ──────────────────────────────────────────────

    [Fact]
    public void ParseRemindLadder_HandlesWhitespaceAndDuplicates()
    {
        var ladder = SubscriptionForecastService.ParseRemindLadder(" 30, 7 , 1 , 7 ");
        ladder.Should().Equal(1, 7, 30);
    }

    [Fact]
    public void ParseRemindLadder_DropsInvalidAndNonPositive()
    {
        var ladder = SubscriptionForecastService.ParseRemindLadder("30,abc,0,-5,7");
        ladder.Should().Equal(7, 30);
    }

    [Fact]
    public void ParseRemindLadder_EmptyReturnsEmpty()
    {
        SubscriptionForecastService.ParseRemindLadder("").Should().BeEmpty();
        SubscriptionForecastService.ParseRemindLadder(null!).Should().BeEmpty();
    }

    // ── StepsCrossingToday ─────────────────────────────────────────────

    [Fact]
    public void StepsCrossingToday_ReturnsMatchingLadderStep()
    {
        // Renewal in 7 days, ladder = [30, 7, 1] → today crosses the "7" step
        var sub = NewSub(
            renewal: Today.AddDays(7),
            remindCsv: "30,7,1");

        var crossing = SubscriptionForecastService.StepsCrossingToday(sub, Today);
        crossing.Should().Equal(7);
    }

    [Fact]
    public void StepsCrossingToday_NoLadder_ReturnsEmpty()
    {
        var sub = NewSub(renewal: Today.AddDays(3), remindCsv: "");
        SubscriptionForecastService.StepsCrossingToday(sub, Today).Should().BeEmpty();
    }
}