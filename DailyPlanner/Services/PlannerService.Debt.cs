using DailyPlanner.Data;
using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Services;

public sealed partial class PlannerService
{
    public async Task<List<Debt>> GetDebtsAsync(bool includeSettled = false, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var query = db.Debts.Include(d => d.Payments).AsNoTracking();
        if (!includeSettled) query = query.Where(d => !d.IsSettled);
        return await query.OrderByDescending(d => d.CreatedDate).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveDebtAsync(Debt debt, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (debt.Id == 0)
        {
            db.Debts.Add(debt);
        }
        else
        {
            db.Debts.Attach(debt);
            db.Entry(debt).State = EntityState.Modified;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveDebtAsync(int debtId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
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
        await using var db = _dbFactory.CreateDbContext();
        db.DebtPayments.Add(payment);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecalcSettledStateAsync(db, payment.DebtId, ct).ConfigureAwait(false);
    }
    public async Task RemoveDebtPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var p = await db.DebtPayments.FindAsync([paymentId], ct).ConfigureAwait(false);
        if (p is null) return;
        var debtId = p.DebtId;
        db.DebtPayments.Remove(p);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecalcSettledStateAsync(db, debtId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Payments are the source of truth for "settled": a fully paid debt closes
    /// itself, dropping below the full amount reopens it. IsSettled used to be an
    /// independent flag — a fully paid debt could hang in the active list forever.
    /// The manual toggle still works for write-offs; it is only revisited here,
    /// when payments actually change.
    /// </summary>
    private static async Task RecalcSettledStateAsync(PlannerDbContext db, int debtId, CancellationToken ct)
    {
        var debt = await db.Debts.Include(d => d.Payments)
            .FirstOrDefaultAsync(d => d.Id == debtId, ct).ConfigureAwait(false);
        if (debt is null || debt.Amount <= 0) return;

        var fullyPaid = debt.Payments.Sum(p => p.Amount) >= debt.Amount;
        if (fullyPaid == debt.IsSettled) return;

        debt.IsSettled = fullyPaid;
        debt.SettledDate = fullyPaid ? DateOnly.FromDateTime(DateTime.Today) : null;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
