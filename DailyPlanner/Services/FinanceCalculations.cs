using DailyPlanner.Models;

namespace DailyPlanner.Services;

/// <summary>Pure math helpers for finance calculations. No side effects.</summary>
public static class FinanceCalculations
{
    public static decimal TotalPaid(IEnumerable<decimal> payments) => payments.Sum();

    public static decimal RemainingDebt(decimal amount, IEnumerable<decimal> payments)
        => amount - TotalPaid(payments);

    public static double DebtProgressPercent(decimal amount, decimal paid)
        => amount > 0 ? Math.Min((double)paid / (double)amount * 100, 100) : 0;

    public static decimal NetWorth(decimal balance, decimal owedToMe, decimal iOwe)
        => balance + owedToMe - iOwe;

    public static double SavingsRatePercent(decimal savings, decimal income)
        => income > 0 ? Math.Max(0, Math.Round((double)(savings / income) * 100, 1)) : 0;

    /// <summary>Normalize recurring payment amount to its monthly equivalent.</summary>
    public static decimal MonthlyEquivalent(PaymentFrequency frequency, decimal amount) => frequency switch
    {
        PaymentFrequency.Monthly => amount,
        PaymentFrequency.Weekly => amount * 4.33m,
        PaymentFrequency.Biweekly => amount * 2.17m,
        PaymentFrequency.Quarterly => amount / 3,
        PaymentFrequency.Yearly => amount / 12,
        _ => 0
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
