using DailyPlanner.Models;
using DailyPlanner.Services;
using FluentAssertions;

namespace DailyPlanner.Tests;

public class FinanceCalculationsTests
{
    // ─── RemainingDebt ──────────────────────────────────────────────

    [Fact]
    public void RemainingDebt_WithNoPayments_ReturnsFullAmount()
    {
        FinanceCalculations.RemainingDebt(1000m, []).Should().Be(1000m);
    }

    [Fact]
    public void RemainingDebt_WithPartialPayments_ReturnsDifference()
    {
        FinanceCalculations.RemainingDebt(1000m, [300m, 200m]).Should().Be(500m);
    }

    [Fact]
    public void RemainingDebt_WithOverpayment_ReturnsNegative()
    {
        FinanceCalculations.RemainingDebt(100m, [150m]).Should().Be(-50m);
    }

    // ─── DebtProgressPercent ────────────────────────────────────────

    [Fact]
    public void DebtProgressPercent_HalfPaid_Returns50()
    {
        FinanceCalculations.DebtProgressPercent(1000m, 500m).Should().Be(50);
    }

    [Fact]
    public void DebtProgressPercent_FullyPaid_Returns100()
    {
        FinanceCalculations.DebtProgressPercent(1000m, 1000m).Should().Be(100);
    }

    [Fact]
    public void DebtProgressPercent_Overpaid_CapsAt100()
    {
        FinanceCalculations.DebtProgressPercent(1000m, 1500m).Should().Be(100);
    }

    [Fact]
    public void DebtProgressPercent_ZeroAmount_ReturnsZero()
    {
        FinanceCalculations.DebtProgressPercent(0m, 100m).Should().Be(0);
    }

    // ─── NetWorth ───────────────────────────────────────────────────

    [Fact]
    public void NetWorth_PositiveBalance_AddsOwedToMeSubtractsIOwe()
    {
        FinanceCalculations.NetWorth(balance: 1000m, owedToMe: 500m, iOwe: 300m)
            .Should().Be(1200m);
    }

    [Fact]
    public void NetWorth_DebtExceedsAssets_ReturnsNegative()
    {
        FinanceCalculations.NetWorth(balance: 100m, owedToMe: 0m, iOwe: 500m)
            .Should().Be(-400m);
    }

    // ─── SavingsRatePercent ─────────────────────────────────────────

    [Fact]
    public void SavingsRatePercent_TwentyPercent()
    {
        FinanceCalculations.SavingsRatePercent(savings: 200m, income: 1000m)
            .Should().Be(20);
    }

    [Fact]
    public void SavingsRatePercent_NegativeSavings_ReturnsZero()
    {
        FinanceCalculations.SavingsRatePercent(savings: -100m, income: 1000m)
            .Should().Be(0);
    }

    [Fact]
    public void SavingsRatePercent_ZeroIncome_ReturnsZero()
    {
        FinanceCalculations.SavingsRatePercent(savings: 100m, income: 0m)
            .Should().Be(0);
    }

    // ─── MonthlyEquivalent ──────────────────────────────────────────

    [Theory]
    [InlineData(PaymentFrequency.Monthly, 100, 100)]
    [InlineData(PaymentFrequency.Quarterly, 300, 100)]
    [InlineData(PaymentFrequency.Yearly, 1200, 100)]
    public void MonthlyEquivalent_ConvertsCorrectly(PaymentFrequency freq, decimal amount, decimal expected)
    {
        FinanceCalculations.MonthlyEquivalent(freq, amount).Should().Be(expected);
    }

    [Fact]
    public void MonthlyEquivalent_Weekly_MultipliesBy4_33()
    {
        FinanceCalculations.MonthlyEquivalent(PaymentFrequency.Weekly, 100m)
            .Should().Be(433m);
    }

    [Fact]
    public void MonthlyEquivalent_Biweekly_MultipliesBy2_17()
    {
        FinanceCalculations.MonthlyEquivalent(PaymentFrequency.Biweekly, 100m)
            .Should().Be(217m);
    }

    // ─── MonthlyObligatory ──────────────────────────────────────────

    [Fact]
    public void MonthlyObligatory_SumsAllActiveExpenses()
    {
        var payments = new List<RecurringPayment>
        {
            new() { Type = FinanceEntryType.Expense, IsActive = true, Frequency = PaymentFrequency.Monthly, Amount = 500m },
            new() { Type = FinanceEntryType.Expense, IsActive = true, Frequency = PaymentFrequency.Yearly, Amount = 1200m },
        };
        FinanceCalculations.MonthlyObligatory(payments).Should().Be(600m);
    }

    [Fact]
    public void MonthlyObligatory_IgnoresIncomeAndInactive()
    {
        var payments = new List<RecurringPayment>
        {
            new() { Type = FinanceEntryType.Income, IsActive = true, Frequency = PaymentFrequency.Monthly, Amount = 5000m },
            new() { Type = FinanceEntryType.Expense, IsActive = false, Frequency = PaymentFrequency.Monthly, Amount = 300m },
            new() { Type = FinanceEntryType.Expense, IsActive = true, Frequency = PaymentFrequency.Monthly, Amount = 100m },
        };
        FinanceCalculations.MonthlyObligatory(payments).Should().Be(100m);
    }

    [Fact]
    public void MonthlyObligatory_EmptyList_ReturnsZero()
    {
        FinanceCalculations.MonthlyObligatory([]).Should().Be(0m);
    }
}
