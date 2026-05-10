using DailyPlanner.Data;
using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Services;

public sealed partial class PlannerService
{
    public async Task<List<FinanceCategory>> GetFinanceCategoriesAsync(FinanceEntryType? type = null, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var query = db.FinanceCategories.AsNoTracking().Where(c => !c.IsArchived);
        if (type is not null) query = query.Where(c => c.Type == type);
        return await query.OrderBy(c => c.Order).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveFinanceCategoryAsync(FinanceCategory category, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (category.Id == 0)
            db.FinanceCategories.Add(category);
        else
            db.FinanceCategories.Update(category);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task ArchiveFinanceCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var cat = await db.FinanceCategories.FindAsync([categoryId], ct).ConfigureAwait(false);
        if (cat is not null)
        {
            cat.IsArchived = true;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
    public async Task<bool> CanDeleteFinanceCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var hasEntries = await db.FinanceEntries.AnyAsync(e => e.CategoryId == categoryId, ct).ConfigureAwait(false);
        var hasPayments = await db.RecurringPayments.AnyAsync(rp => rp.CategoryId == categoryId, ct).ConfigureAwait(false);
        return !hasEntries && !hasPayments;
    }
    public async Task<bool> RemoveFinanceCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var hasEntries = await db.FinanceEntries.AnyAsync(e => e.CategoryId == categoryId, ct).ConfigureAwait(false);
        var hasPayments = await db.RecurringPayments.AnyAsync(rp => rp.CategoryId == categoryId, ct).ConfigureAwait(false);
        if (hasEntries || hasPayments) return false;

        var entity = await db.FinanceCategories.FindAsync([categoryId], ct).ConfigureAwait(false);
        if (entity is null) return false;
        db.FinanceCategories.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
    private static readonly (string SeedKey, string Icon, string Color, FinanceEntryType Type, int Order)[] SeedDefinitions =
    [
        ("CatSalary",        "\uD83D\uDCBC", "#34D399", FinanceEntryType.Income,  1),
        ("CatFreelance",     "\uD83D\uDCBB", "#38BDF8", FinanceEntryType.Income,  2),
        ("CatGifts",         "\uD83C\uDF81", "#A78BFA", FinanceEntryType.Income,  3),
        ("CatInvestments",   "\uD83D\uDCC8", "#FBBF24", FinanceEntryType.Income,  4),
        ("CatOtherIncome",   "\u2795",       "#6EE7B7", FinanceEntryType.Income,  5),
        ("CatFood",          "\uD83C\uDF54", "#FB923C", FinanceEntryType.Expense, 1),
        ("CatTransport",     "\uD83D\uDE97", "#38BDF8", FinanceEntryType.Expense, 2),
        ("CatHousing",       "\uD83C\uDFE0", "#A78BFA", FinanceEntryType.Expense, 3),
        ("CatEntertainment", "\uD83C\uDFAE", "#F472B6", FinanceEntryType.Expense, 4),
        ("CatHealth",        "\uD83D\uDC8A", "#34D399", FinanceEntryType.Expense, 5),
        ("CatClothing",      "\uD83D\uDC55", "#FBBF24", FinanceEntryType.Expense, 6),
        ("CatSubscriptions", "\uD83D\uDCF1", "#FB7185", FinanceEntryType.Expense, 7),
        ("CatEducation",     "\uD83D\uDCDA", "#818CF8", FinanceEntryType.Expense, 8),
        ("CatOtherExpense",  "\u2796",       "#585878", FinanceEntryType.Expense, 9),
    ];

    // Maps legacy seeded names (any language) back to their stable SeedKey, so
    // pre-3.25.0 databases without the new column can be migrated on first launch.
    private static readonly string[] LegacySeedNameKeys = ["en", "ru", "es", "fr"];

    public async Task SeedFinanceCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();

        var existing = await db.FinanceCategories.ToListAsync(ct).ConfigureAwait(false);
        if (existing.Count == 0)
        {
            foreach (var def in SeedDefinitions)
            {
                db.FinanceCategories.Add(new FinanceCategory
                {
                    SeedKey = def.SeedKey,
                    Name = Loc.Get(def.SeedKey),
                    Icon = def.Icon,
                    Color = def.Color,
                    Type = def.Type,
                    Order = def.Order,
                });
            }
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var changed = false;
        foreach (var cat in existing)
        {
            // Backfill SeedKey for rows seeded before 3.25.0 by matching the
            // current Name against the same key in any supported language.
            if (string.IsNullOrEmpty(cat.SeedKey))
            {
                foreach (var def in SeedDefinitions)
                {
                    foreach (var lang in LegacySeedNameKeys)
                    {
                        if (Loc.GetIn(lang, def.SeedKey) == cat.Name)
                        {
                            cat.SeedKey = def.SeedKey;
                            changed = true;
                            break;
                        }
                    }
                    if (cat.SeedKey is not null) break;
                }
            }

            // For seeded rows, keep Name in sync with the active language.
            if (!string.IsNullOrEmpty(cat.SeedKey))
            {
                var localized = Loc.Get(cat.SeedKey);
                if (!string.Equals(cat.Name, localized, StringComparison.Ordinal))
                {
                    cat.Name = localized;
                    changed = true;
                }
            }
        }
        if (changed) await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task<List<FinanceEntry>> GetFinanceEntriesAsync(DateOnly from, DateOnly to, FinanceEntryType? type = null, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var query = db.FinanceEntries.AsNoTracking().Include(e => e.Category).Where(e => e.Date >= from && e.Date <= to);
        if (type is not null) query = query.Where(e => e.Type == type);
        return await query.OrderBy(e => e.Date).ThenBy(e => e.Id).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveFinanceEntryAsync(FinanceEntry entry, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();

        if (entry.CategoryId > 0)
        {
            var isArchived = await db.FinanceCategories
                .AsNoTracking()
                .Where(c => c.Id == entry.CategoryId)
                .Select(c => (bool?)c.IsArchived)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (isArchived == true) return;
        }

        if (entry.Id == 0)
        {
            db.FinanceEntries.Add(entry);
        }
        else
        {
            db.FinanceEntries.Attach(entry);
            db.Entry(entry).State = EntityState.Modified;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveFinanceEntryAsync(int entryId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var entry = await db.FinanceEntries.FindAsync([entryId], ct).ConfigureAwait(false);
        if (entry is not null) { db.FinanceEntries.Remove(entry); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task<List<FinanceBudget>> GetBudgetsAsync(string monthYear, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.FinanceBudgets.AsNoTracking().Include(b => b.Category).Where(b => b.MonthYear == monthYear).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveBudgetAsync(FinanceBudget budget, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (budget.Id == 0)
        {
            db.FinanceBudgets.Add(budget);
        }
        else
        {
            db.FinanceBudgets.Attach(budget);
            db.Entry(budget).State = EntityState.Modified;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveBudgetAsync(int budgetId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var b = await db.FinanceBudgets.FindAsync([budgetId], ct).ConfigureAwait(false);
        if (b is not null) { db.FinanceBudgets.Remove(b); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task<List<RecurringPayment>> GetRecurringPaymentsAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var query = db.RecurringPayments.AsNoTracking().Include(rp => rp.Category).AsQueryable();
        if (activeOnly) query = query.Where(rp => rp.IsActive);
        return await query.OrderBy(rp => rp.Type).ThenBy(rp => rp.Name).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveRecurringPaymentAsync(RecurringPayment payment, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (payment.Id == 0)
        {
            db.RecurringPayments.Add(payment);
        }
        else
        {
            db.RecurringPayments.Attach(payment);
            db.Entry(payment).State = EntityState.Modified;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveRecurringPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var rp = await db.RecurringPayments.FindAsync([paymentId], ct).ConfigureAwait(false);
        if (rp is not null) { db.RecurringPayments.Remove(rp); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    /// <summary>
    /// Materializes pending FinanceEntry rows for every active AutoCreate
    /// RecurringPayment that has an occurrence in <paramref name="from"/>..
    /// <paramref name="to"/>. Each created row is IsPaid=false ("pending")
    /// so the user confirms with a one-click "Mark Paid" in the UI.
    ///
    /// Uses SubscriptionForecastService.EnumerateOccurrences as the single
    /// source of truth for "when does this recur" — same logic the renewal
    /// reminder service uses, so a payment that fires a reminder also gets
    /// a pending entry materialized.
    /// </summary>
    public async Task GenerateRecurringEntriesAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from) return;

        await using var db = _dbFactory.CreateDbContext();
        var payments = await db.RecurringPayments
            .Where(rp => rp.IsActive && rp.AutoCreate && rp.StartDate <= to && (rp.EndDate == null || rp.EndDate >= from))
            .ToListAsync(ct).ConfigureAwait(false);

        var existingKeys = (await db.FinanceEntries
            .Where(e => e.RecurringPaymentId != null && e.Date >= from && e.Date <= to)
            .Select(e => new { e.RecurringPaymentId, e.Date })
            .ToListAsync(ct).ConfigureAwait(false))
            .ToHashSet();

        var window = to.DayNumber - from.DayNumber + 1;
        var newEntries = new List<FinanceEntry>();

        foreach (var rp in payments)
        {
            foreach (var occ in SubscriptionForecastService.EnumerateOccurrences(rp, from, window))
            {
                if (occ.Date > to) break;
                if (!existingKeys.Add(new { RecurringPaymentId = (int?)rp.Id, Date = occ.Date })) continue;

                newEntries.Add(new FinanceEntry
                {
                    Date = occ.Date,
                    CategoryId = rp.CategoryId,
                    Type = rp.Type,
                    Amount = rp.Amount,
                    Currency = rp.Currency,
                    Description = rp.Name,
                    IsRecurring = true,
                    RecurringPaymentId = rp.Id,
                    IsPaid = false
                });
            }
        }

        if (newEntries.Count > 0)
        {
            db.FinanceEntries.AddRange(newEntries);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
    public async Task<List<CategoryBreakdownItem>> GetExpensesByCategoryAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var entries = await db.FinanceEntries
            .Include(e => e.Category)
            .Where(e => e.Type == FinanceEntryType.Expense && e.Date >= from && e.Date <= to)
            .ToListAsync(ct).ConfigureAwait(false);

        return entries
            .GroupBy(e => e.CategoryId)
            .Select(g =>
            {
                var cat = g.First().Category;
                return new CategoryBreakdownItem(
                    g.Key, cat?.Name ?? string.Empty, cat?.Icon ?? string.Empty,
                    cat?.Color ?? "#cba6f7", g.Sum(e => e.Amount));
            })
            .OrderByDescending(x => x.Amount)
            .ToList();
    }
    public async Task<List<MonthlyFinanceSummary>> GetMonthlyTotalsAsync(int months, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var cutoff = DateOnly.FromDateTime(DateTime.Today).AddMonths(-months + 1);
        cutoff = new DateOnly(cutoff.Year, cutoff.Month, 1);

        var rawEntries = await db.FinanceEntries
            .Where(e => e.Date >= cutoff)
            .Select(e => new { e.Date, e.Type, e.Amount })
            .ToListAsync(ct).ConfigureAwait(false);

        var entries = rawEntries
            .GroupBy(e => new { e.Date.Year, e.Date.Month, e.Type })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Type, Total = g.Sum(e => e.Amount) })
            .ToList();

        var result = new List<MonthlyFinanceSummary>();
        for (var i = 0; i < months; i++)
        {
            var d = DateOnly.FromDateTime(DateTime.Today).AddMonths(-months + 1 + i);
            var y = d.Year;
            var m = d.Month;
            var inc = entries.FirstOrDefault(e => e.Year == y && e.Month == m && e.Type == FinanceEntryType.Income)?.Total ?? 0;
            var exp = entries.FirstOrDefault(e => e.Year == y && e.Month == m && e.Type == FinanceEntryType.Expense)?.Total ?? 0;
            result.Add(new MonthlyFinanceSummary(y, m, inc, exp));
        }

        return result;
    }
    public async Task<List<FinancialGoal>> GetFinancialGoalsAsync(CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.FinancialGoals.OrderBy(g => g.Order).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveFinancialGoalAsync(FinancialGoal goal, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (goal.Id == 0) db.FinancialGoals.Add(goal);
        else db.FinancialGoals.Update(goal);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveFinancialGoalAsync(int goalId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var goal = await db.FinancialGoals.FindAsync([goalId], ct).ConfigureAwait(false);
        if (goal is not null) { db.FinancialGoals.Remove(goal); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task<List<Account>> GetAccountsAsync(CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.Accounts.Where(a => !a.IsArchived).OrderBy(a => a.Order).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveAccountAsync(Account account, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (account.Id == 0) db.Accounts.Add(account);
        else db.Accounts.Update(account);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveAccountAsync(int accountId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var account = await db.Accounts.FindAsync([accountId], ct).ConfigureAwait(false);
        if (account is not null) { db.Accounts.Remove(account); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task<decimal> GetAccountBalanceAsync(int accountId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var account = await db.Accounts.FindAsync([accountId], ct).ConfigureAwait(false);
        if (account is null) return 0;

        var income = await db.FinanceEntries
            .Where(e => e.AccountId == accountId && e.Type == FinanceEntryType.Income)
            .SumAsync(e => e.Amount, ct).ConfigureAwait(false);
        var expense = await db.FinanceEntries
            .Where(e => e.AccountId == accountId && e.Type == FinanceEntryType.Expense)
            .SumAsync(e => e.Amount, ct).ConfigureAwait(false);
        var transfersIn = await db.AccountTransfers
            .Where(t => t.ToAccountId == accountId)
            .SumAsync(t => t.Amount, ct).ConfigureAwait(false);
        var transfersOut = await db.AccountTransfers
            .Where(t => t.FromAccountId == accountId)
            .SumAsync(t => t.Amount, ct).ConfigureAwait(false);

        return account.InitialBalance + income - expense + transfersIn - transfersOut;
    }
    public async Task<List<AccountTransfer>> GetAccountTransfersAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.AccountTransfers
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Where(t => t.Date >= from && t.Date <= to)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveAccountTransferAsync(AccountTransfer transfer, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (transfer.Id == 0) db.AccountTransfers.Add(transfer);
        else db.AccountTransfers.Update(transfer);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveAccountTransferAsync(int transferId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var transfer = await db.AccountTransfers.FindAsync([transferId], ct).ConfigureAwait(false);
        if (transfer is not null) { db.AccountTransfers.Remove(transfer); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task<List<FinanceEntry>> GetSplitEntriesAsync(int parentEntryId, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.FinanceEntries
            .Include(e => e.Category)
            .Where(e => e.ParentEntryId == parentEntryId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task<List<ForecastDay>> GetBalanceForecastAsync(int days, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var endDate = today.AddDays(days);

        // Current balance (all time)
        var allIncome = await db.FinanceEntries
            .Where(e => e.Type == FinanceEntryType.Income && e.Date <= today)
            .SumAsync(e => e.Amount, ct).ConfigureAwait(false);
        var allExpense = await db.FinanceEntries
            .Where(e => e.Type == FinanceEntryType.Expense && e.Date <= today)
            .SumAsync(e => e.Amount, ct).ConfigureAwait(false);
        var currentBalance = allIncome - allExpense;

        // Already-planned entries in the future
        var futureEntries = await db.FinanceEntries
            .Where(e => e.Date > today && e.Date <= endDate)
            .ToListAsync(ct).ConfigureAwait(false);

        // Active recurring payments
        var recurring = await db.RecurringPayments
            .Where(rp => rp.IsActive && (rp.EndDate == null || rp.EndDate >= today))
            .ToListAsync(ct).ConfigureAwait(false);

        var result = new List<ForecastDay>();
        var runningBalance = currentBalance;

        for (var d = today; d <= endDate; d = d.AddDays(1))
        {
            decimal dayIncome = 0, dayExpense = 0;

            // Future entries
            foreach (var e in futureEntries.Where(e => e.Date == d))
            {
                if (e.Type == FinanceEntryType.Income) dayIncome += e.Amount;
                else dayExpense += e.Amount;
            }

            // Recurring payments
            foreach (var rp in recurring)
            {
                if (MatchesRecurringDate(rp, d))
                {
                    if (rp.Type == FinanceEntryType.Income) dayIncome += rp.Amount;
                    else dayExpense += rp.Amount;
                }
            }

            if (d > today) runningBalance += dayIncome - dayExpense;
            result.Add(new ForecastDay(d, dayIncome, dayExpense, runningBalance));
        }

        return result;
    }
    private static bool MatchesRecurringDate(RecurringPayment rp, DateOnly date)
    {
        if (date < rp.StartDate) return false;
        if (rp.EndDate is not null && date > rp.EndDate) return false;

        return rp.Frequency switch
        {
            PaymentFrequency.Monthly => rp.DayOfMonth is not null && date.Day == rp.DayOfMonth,
            PaymentFrequency.Weekly => rp.DayOfWeek is not null && date.DayOfWeek == rp.DayOfWeek,
            PaymentFrequency.Biweekly => rp.DayOfWeek is not null && date.DayOfWeek == rp.DayOfWeek
                && Math.Abs(date.ToDateTime(TimeOnly.MinValue).Subtract(rp.StartDate.ToDateTime(TimeOnly.MinValue)).Days) % 14 == 0,
            PaymentFrequency.Quarterly => rp.DayOfMonth is not null && date.Day == rp.DayOfMonth
                && ((date.Year - rp.StartDate.Year) * 12 + date.Month - rp.StartDate.Month) % 3 == 0,
            PaymentFrequency.Yearly => rp.DayOfMonth is not null && date.Day == rp.DayOfMonth
                && date.Month == rp.StartDate.Month,
            _ => false
        };
    }
    public async Task<List<CashflowDay>> GetCashflowCalendarAsync(int year, int month, CancellationToken ct = default)
    {
        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        await using var db = _dbFactory.CreateDbContext();
        var entries = await db.FinanceEntries
            .Where(e => e.Date >= firstDay && e.Date <= lastDay)
            .ToListAsync(ct).ConfigureAwait(false);

        var recurring = await db.RecurringPayments
            .Where(rp => rp.IsActive)
            .ToListAsync(ct).ConfigureAwait(false);

        var result = new List<CashflowDay>();
        for (var d = firstDay; d <= lastDay; d = d.AddDays(1))
        {
            var dayEntries = entries.Where(e => e.Date == d).ToList();
            var income = dayEntries.Where(e => e.Type == FinanceEntryType.Income).Sum(e => e.Amount);
            var expense = dayEntries.Where(e => e.Type == FinanceEntryType.Expense).Sum(e => e.Amount);

            // Add recurring that aren't already generated
            foreach (var rp in recurring.Where(rp => MatchesRecurringDate(rp, d)))
            {
                if (!entries.Any(e => e.RecurringPaymentId == rp.Id && e.Date == d))
                {
                    if (rp.Type == FinanceEntryType.Income) income += rp.Amount;
                    else expense += rp.Amount;
                }
            }

            result.Add(new CashflowDay(d, income, expense));
        }

        return result;
    }
    public async Task<int> ImportFinanceEntriesFromCsvAsync(string filePath, CancellationToken ct = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var lines = await System.IO.File.ReadAllLinesAsync(filePath, System.Text.Encoding.UTF8, ct).ConfigureAwait(false);
        if (lines.Length < 2) return 0;

        var defaultCategory = await db.FinanceCategories.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (defaultCategory is null) return 0;

        var imported = 0;
        // Expected CSV: Date,Description,Amount (positive = income, negative = expense)
        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 3) continue;

            if (!DateOnly.TryParse(parts[0], out var date)) continue;
            if (!decimal.TryParse(parts[2], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount)
                && !decimal.TryParse(parts[2], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture, out amount)) continue;

            var entry = new FinanceEntry
            {
                Date = date,
                Description = parts[1],
                Amount = Math.Abs(amount),
                Type = amount >= 0 ? FinanceEntryType.Income : FinanceEntryType.Expense,
                CategoryId = defaultCategory.Id,
                IsPaid = true
            };
            db.FinanceEntries.Add(entry);
            imported++;
        }

        if (imported > 0) await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return imported;
    }
}
