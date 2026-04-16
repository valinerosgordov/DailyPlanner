using DailyPlanner.Data;
using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Services;

public sealed partial class PlannerService
{
    public async Task<List<IncomeSource>> GetIncomeSourcesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var query = db.IncomeSources.Include(s => s.Payments).AsQueryable();
        if (activeOnly) query = query.Where(s => s.IsActive);
        return await query.OrderBy(s => s.Order).ThenBy(s => s.Name).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveIncomeSourceAsync(IncomeSource source, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (source.Id == 0)
            db.IncomeSources.Add(source);
        else
            db.IncomeSources.Update(source);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveIncomeSourceAsync(int sourceId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var source = await db.IncomeSources.FindAsync([sourceId], ct).ConfigureAwait(false);
        if (source is not null) { db.IncomeSources.Remove(source); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task SaveIncomeSourcePaymentAsync(IncomeSourcePayment payment, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (payment.Id == 0)
            db.IncomeSourcePayments.Add(payment);
        else
            db.IncomeSourcePayments.Update(payment);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveIncomeSourcePaymentAsync(int paymentId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var payment = await db.IncomeSourcePayments.FindAsync([paymentId], ct).ConfigureAwait(false);
        if (payment is not null) { db.IncomeSourcePayments.Remove(payment); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    /// <summary>
    /// Calculate expected vs received income from income sources for a given month.
    /// Matches FinanceEntry by source name + date to determine paid status.
    /// </summary>
    public async Task<List<IncomeSourceStatus>> GetIncomeSourceStatusAsync(int year, int month, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var sources = await db.IncomeSources.Include(s => s.Payments)
            .Where(s => s.IsActive)
            .ToListAsync(ct).ConfigureAwait(false);

        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var incomeEntries = await db.FinanceEntries
            .Where(e => e.Type == FinanceEntryType.Income && e.Date >= firstDay && e.Date <= lastDay)
            .ToListAsync(ct).ConfigureAwait(false);

        var result = new List<IncomeSourceStatus>();
        foreach (var source in sources)
        {
            decimal expected = 0, received = 0;
            var paymentStatuses = new List<IncomePaymentStatus>();

            foreach (var p in source.Payments.OrderBy(p => p.DayOfMonth))
            {
                expected += p.Amount;
                var day = Math.Min(p.DayOfMonth, DateTime.DaysInMonth(year, month));
                var payDate = new DateOnly(year, month, day);
                // Match by description containing source name or by date proximity
                var matched = incomeEntries.FirstOrDefault(e =>
                    e.Date == payDate && Math.Abs(e.Amount - p.Amount) < 0.01m);
                var isPaid = matched is not null;
                if (isPaid) received += p.Amount;

                paymentStatuses.Add(new IncomePaymentStatus(
                    p.Id, p.DayOfMonth, p.Amount, p.Description, payDate, isPaid));
            }

            result.Add(new IncomeSourceStatus(
                source.Id, source.Name, source.ClientName, source.Icon, source.Color,
                source.TotalMonthlyAmount, expected, received, paymentStatuses));
        }

        return result;
    }
}
