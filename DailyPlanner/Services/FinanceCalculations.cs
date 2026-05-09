using DailyPlanner.Models;

namespace DailyPlanner.Services;

/// <summary>Pure math helpers for finance calculations. No side effects.</summary>
public static class FinanceCalculations
{
    /// <summary>Average weeks per calendar month (52 weeks / 12 months ≈ 4.333), rounded to 2dp.</summary>
    public const decimal WeeksPerMonth = 4.33m;

    /// <summary>Average bi-weekly periods per calendar month (26 / 12 ≈ 2.167), rounded to 2dp.</summary>
    public const decimal BiweeksPerMonth = 2.17m;

    public static decimal TotalPaid(IEnumerable<decimal> payments) => payments.Sum();

    public static decimal RemainingDebt(decimal amount, IEnumerable<decimal> payments)
        => amount - TotalPaid(payments);

    public static double DebtProgressPercent(decimal amount, decimal paid)
        => amount > 0 ? Math.Min((double)paid / (double)amount * 100, 100) : 0;

    public static decimal NetWorth(decimal balance, decimal owedToMe, decimal iOwe)
        => balance + owedToMe - iOwe;

    public static double SavingsRatePercent(decimal savings, decimal income)
        => income > 0 ? Math.Max(0, Math.Round((double)(savings / income) * 100, 1)) : 0;

    /// <summary>
    /// Normalize a recurring payment amount to its monthly equivalent.
    /// Throws on unknown frequencies — adding a new <see cref="PaymentFrequency"/>
    /// value should fail loudly here so callers can be updated, instead of silently
    /// counting that payment as zero (the previous behaviour).
    /// </summary>
    public static decimal MonthlyEquivalent(PaymentFrequency frequency, decimal amount) => frequency switch
    {
        PaymentFrequency.Monthly => amount,
        PaymentFrequency.Weekly => amount * WeeksPerMonth,
        PaymentFrequency.Biweekly => amount * BiweeksPerMonth,
        PaymentFrequency.Quarterly => amount / 3,
        PaymentFrequency.Yearly => amount / 12,
        _ => throw new ArgumentOutOfRangeException(
            nameof(frequency), frequency,
            $"Unknown payment frequency '{frequency}'. Add a case in MonthlyEquivalent.")
    };

    /// <summary>Sum of monthly-equivalent amounts for all active expense recurring payments.</summary>
    public static decimal MonthlyObligatory(IEnumerable<RecurringPayment> payments)
    {
        decimal total = 0;
        foreach (var p in payments)
        {
            if (p.Type != FinanceEntryType.Expense || !p.IsActive) continue;
            total += MonthlyEquivalent(p.Frequency, p.Amount);
        }
        return Math.Round(total, 2);
    }
}