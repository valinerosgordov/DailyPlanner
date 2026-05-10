using DailyPlanner.Models;

namespace DailyPlanner.Services;

/// <summary>
/// Pure-logic forecasting of upcoming recurring payments. No DB access, no
/// HTTP, no DI — call directly from the VM after loading RecurringPayments
/// from the DbContext. All functions are deterministic given a "now" date,
/// which makes them trivially testable.
/// </summary>
public static class SubscriptionForecastService
{
    /// <summary>Status of a forecasted occurrence relative to today.</summary>
    public enum OccurrenceStatus
    {
        /// <summary>Future occurrence within the forecast window.</summary>
        Pending,
        /// <summary>Due today.</summary>
        DueToday,
        /// <summary>Due within RemindDaysBefore window.</summary>
        DueSoon,
        /// <summary>Past due, not paid.</summary>
        Overdue,
        /// <summary>Trial expires.</summary>
        TrialEnding
    }

    /// <summary>Single forecasted occurrence of a recurring payment.</summary>
    public sealed record Occurrence(
        RecurringPayment Payment,
        DateOnly Date,
        OccurrenceStatus Status,
        int DaysFromNow);

    /// <summary>
    /// Generates all occurrences for the given payment in <paramref name="window"/>
    /// days from <paramref name="today"/>. Caps at 366 days to avoid runaway loops.
    /// </summary>
    public static IEnumerable<Occurrence> EnumerateOccurrences(
        RecurringPayment payment, DateOnly today, int window)
    {
        if (!payment.IsActive) yield break;
        window = Math.Clamp(window, 1, 366);

        var end = today.AddDays(window);
        var cursor = NextOccurrenceFrom(payment, today);

        while (cursor.HasValue && cursor.Value <= end)
        {
            // Respect EndDate (subscription terminated)
            if (payment.EndDate.HasValue && cursor.Value > payment.EndDate.Value) yield break;

            var daysFromNow = cursor.Value.DayNumber - today.DayNumber;
            var status = ClassifyStatus(payment, cursor.Value, today);

            yield return new Occurrence(payment, cursor.Value, status, daysFromNow);

            cursor = StepForward(payment, cursor.Value);
        }
    }

    /// <summary>
    /// First occurrence on or after <paramref name="today"/>. Returns null if the
    /// payment has ended or has no plausible next date.
    /// </summary>
    public static DateOnly? NextOccurrenceFrom(RecurringPayment payment, DateOnly today)
    {
        if (!payment.IsActive) return null;
        if (payment.EndDate.HasValue && payment.EndDate.Value < today) return null;

        // Use NextRenewalDate if set — overrides cadence (Timeweb-yearly case).
        if (payment.NextRenewalDate.HasValue && payment.NextRenewalDate.Value >= today)
            return payment.NextRenewalDate;

        // Walk forward from StartDate by Frequency until we reach today.
        var cursor = payment.StartDate;
        while (cursor < today)
        {
            var next = StepForward(payment, cursor);
            if (next is null || next.Value <= cursor) return null; // safety stop
            cursor = next.Value;
        }
        return cursor;
    }

    private static DateOnly? StepForward(RecurringPayment payment, DateOnly from)
    {
        // If BillingIntervalMonths is set (>= 1) and the payment is a
        // commitment-window (Timeweb yearly etc.), step by that. Otherwise
        // honour Frequency.
        if (payment.BillingIntervalMonths > 1)
            return from.AddMonths(payment.BillingIntervalMonths);

        return payment.Frequency switch
        {
            PaymentFrequency.Weekly => from.AddDays(7),
            PaymentFrequency.Biweekly => from.AddDays(14),
            PaymentFrequency.Monthly => SnapToDayOfMonth(from.AddMonths(1), payment.DayOfMonth),
            PaymentFrequency.Quarterly => from.AddMonths(3),
            PaymentFrequency.Yearly => from.AddMonths(12),
            _ => null
        };
    }

    /// <summary>
    /// For monthly with DayOfMonth set, snap to that day (clamping for short
    /// months — 31st becomes 28/29/30). Mirrors how most billing systems work.
    /// </summary>
    private static DateOnly SnapToDayOfMonth(DateOnly date, int? dayOfMonth)
    {
        if (!dayOfMonth.HasValue) return date;
        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        var day = Math.Min(dayOfMonth.Value, daysInMonth);
        return new DateOnly(date.Year, date.Month, day);
    }

    private static OccurrenceStatus ClassifyStatus(RecurringPayment payment, DateOnly date, DateOnly today)
    {
        var diff = date.DayNumber - today.DayNumber;
        if (diff < 0) return OccurrenceStatus.Overdue;
        if (diff == 0) return OccurrenceStatus.DueToday;
        if (diff <= payment.RemindDaysBefore && payment.RemindDaysBefore > 0) return OccurrenceStatus.DueSoon;
        return OccurrenceStatus.Pending;
    }

    // === Aggregates ===

    /// <summary>
    /// Sum of EffectiveMonthlyCost for all ACTIVE subscriptions, currency-aware
    /// only if a converter is provided. Without a converter, sums Amount/Months
    /// in mixed currencies (caller's problem — usually only OK for single-currency).
    /// </summary>
    public static decimal MonthlySubscriptionBurden(
        IEnumerable<RecurringPayment> payments,
        Func<decimal, string, decimal>? toBase = null)
    {
        decimal total = 0;
        foreach (var p in payments)
        {
            if (!p.IsActive || !p.IsSubscription) continue;
            var months = Math.Max(1, p.BillingIntervalMonths);
            var monthly = p.Amount / months;
            total += toBase is null ? monthly : toBase(monthly, p.Currency);
        }
        return Math.Round(total, 2);
    }

    /// <summary>
    /// Annual savings vs paying every subscription at its ListPriceMonthly.
    /// Skips subscriptions without a ListPriceMonthly set.
    /// </summary>
    public static decimal AnnualSavingsVsMonthly(
        IEnumerable<RecurringPayment> payments,
        Func<decimal, string, decimal>? toBase = null)
    {
        decimal total = 0;
        foreach (var p in payments)
        {
            if (!p.IsActive || !p.IsSubscription || p.ListPriceMonthly is null) continue;
            var monthlyEffective = p.Amount / Math.Max(1, p.BillingIntervalMonths);
            var savedMonthly = p.ListPriceMonthly.Value - monthlyEffective;
            if (savedMonthly <= 0) continue; // no discount (or worse)
            var annual = savedMonthly * 12;
            total += toBase is null ? annual : toBase(annual, p.Currency);
        }
        return Math.Round(total, 2);
    }

    /// <summary>
    /// Parses the RenewalRemindDaysBefore CSV ("30,7,1") into a sorted set of
    /// positive ints. Invalid entries are silently dropped (forgiving parser —
    /// user-edited string, no point throwing).
    /// </summary>
    public static IReadOnlyList<int> ParseRemindLadder(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new SortedSet<int>();
        foreach (var p in parts)
        {
            if (int.TryParse(p, out var d) && d > 0) result.Add(d);
        }
        return result.ToList();
    }

    /// <summary>
    /// Returns the day-count steps in the ladder that match the current
    /// distance-to-renewal. E.g. ladder=[30,7,1], days=7 → returns [7].
    /// Used by the reminder service to fire toasts on exact crossings.
    /// </summary>
    public static IReadOnlyList<int> StepsCrossingToday(RecurringPayment payment, DateOnly today)
    {
        if (!payment.NextRenewalDate.HasValue) return [];
        var ladder = ParseRemindLadder(payment.RenewalRemindDaysBefore);
        if (ladder.Count == 0) return [];
        var diff = payment.NextRenewalDate.Value.DayNumber - today.DayNumber;
        return ladder.Where(d => d == diff).ToList();
    }
}