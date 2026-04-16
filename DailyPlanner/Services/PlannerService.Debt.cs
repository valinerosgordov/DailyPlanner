using DailyPlanner.Data;
using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Services;

public sealed partial class PlannerService
{
    public async Task<List<Debt>> GetDebtsAsync(bool includeSettled = false, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var query = db.Debts.Include(d => d.Payments).AsNoTracking();
        if (!includeSettled) query = query.Where(d => !d.IsSettled);
        return await query.OrderByDescending(d => d.CreatedDate).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveDebtAsync(Debt debt, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var savedPayments = debt.Payments;
        debt.Payments = [];

        if (debt.Id == 0)
            db.Debts.Add(debt);
        else
            db.Debts.Update(debt);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        debt.Payments = savedPayments;
    }
    public async Task RemoveDebtAsync(int debtId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var debt = await db.Debts.Include(d => d.Payments).FirstOrDefaultAsync(d => d.Id == debtId, ct).ConfigureAwait(false);
        if (debt is not null)
        {
            db.DebtPayments.RemoveRange(debt.Payments);
            db.Debts.Remove(debt);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
    public async Task AddDebtPaymentAsync(DebtPayment payment, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        db.DebtPayments.Add(payment);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveDebtPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var p = await db.DebtPayments.FindAsync([paymentId], ct).ConfigureAwait(false);
        if (p is not null) { db.DebtPayments.Remove(p); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
}
