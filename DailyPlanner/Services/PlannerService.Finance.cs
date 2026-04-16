using DailyPlanner.Data;
using DailyPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyPlanner.Services;

public sealed partial class PlannerService
{
    public async Task<List<FinanceCategory>> GetFinanceCategoriesAsync(FinanceEntryType? type = null, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var query = db.FinanceCategories.Where(c => !c.IsArchived);
        if (type is not null) query = query.Where(c => c.Type == type);
        return await query.OrderBy(c => c.Order).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveFinanceCategoryAsync(FinanceCategory category, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (category.Id == 0)
            db.FinanceCategories.Add(category);
        else
            db.FinanceCategories.Update(category);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task ArchiveFinanceCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var cat = await db.FinanceCategories.FindAsync([categoryId], ct).ConfigureAwait(false);
        if (cat is not null)
        {
            cat.IsArchived = true;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
    public async Task<bool> CanDeleteFinanceCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var hasEntries = await db.FinanceEntries.AnyAsync(e => e.CategoryId == categoryId, ct).ConfigureAwait(false);
        var hasPayments = await db.RecurringPayments.AnyAsync(rp => rp.CategoryId == categoryId, ct).ConfigureAwait(false);
        return !hasEntries && !hasPayments;
    }
    public async Task<bool> RemoveFinanceCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var hasEntries = await db.FinanceEntries.AnyAsync(e => e.CategoryId == categoryId, ct).ConfigureAwait(false);
        var hasPayments = await db.RecurringPayments.AnyAsync(rp => rp.CategoryId == categoryId, ct).ConfigureAwait(false);
        if (hasEntries || hasPayments) return false;

        var entity = await db.FinanceCategories.FindAsync([categoryId], ct).ConfigureAwait(false);
        if (entity is null) return false;
        db.FinanceCategories.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
    private static readonly Dictionary<string, string> SeedCategoryKeys = new()
    {
        ["Salary"] = "CatSalary", ["Freelance"] = "CatFreelance", ["Gifts"] = "CatGifts",
        ["Investments"] = "CatInvestments", ["Other Income"] = "CatOtherIncome",
        ["Food"] = "CatFood", ["Transport"] = "CatTransport", ["Housing"] = "CatHousing",
        ["Entertainment"] = "CatEntertainment", ["Health"] = "CatHealth",
        ["Clothing"] = "CatClothing", ["Subscriptions"] = "CatSubscriptions",
        ["Education"] = "CatEducation", ["Other Expense"] = "CatOtherExpense",
    };
    public async Task SeedFinanceCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();

                if (!await db.FinanceCategories.AnyAsync(ct).ConfigureAwait(false))
        {
            var categories = new List<FinanceCategory>
            {
                new() { Name = Loc.Get("CatSalary"), Icon = "\uD83D\uDCBC", Color = "#34D399", Type = FinanceEntryType.Income, Order = 1 },
                new() { Name = Loc.Get("CatFreelance"), Icon = "\uD83D\uDCBB", Color = "#38BDF8", Type = FinanceEntryType.Income, Order = 2 },
                new() { Name = Loc.Get("CatGifts"), Icon = "\uD83C\uDF81", Color = "#A78BFA", Type = FinanceEntryType.Income, Order = 3 },
                new() { Name = Loc.Get("CatInvestments"), Icon = "\uD83D\uDCC8", Color = "#FBBF24", Type = FinanceEntryType.Income, Order = 4 },
                new() { Name = Loc.Get("CatOtherIncome"), Icon = "\u2795", Color = "#6EE7B7", Type = FinanceEntryType.Income, Order = 5 },
                new() { Name = Loc.Get("CatFood"), Icon = "\uD83C\uDF54", Color = "#FB923C", Type = FinanceEntryType.Expense, Order = 1 },
                new() { Name = Loc.Get("CatTransport"), Icon = "\uD83D\uDE97", Color = "#38BDF8", Type = FinanceEntryType.Expense, Order = 2 },
                new() { Name = Loc.Get("CatHousing"), Icon = "\uD83C\uDFE0", Color = "#A78BFA", Type = FinanceEntryType.Expense, Order = 3 },
                new() { Name = Loc.Get("CatEntertainment"), Icon = "\uD83C\uDFAE", Color = "#F472B6", Type = FinanceEntryType.Expense, Order = 4 },
                new() { Name = Loc.Get("CatHealth"), Icon = "\uD83D\uDC8A", Color = "#34D399", Type = FinanceEntryType.Expense, Order = 5 },
                new() { Name = Loc.Get("CatClothing"), Icon = "\uD83D\uDC55", Color = "#FBBF24", Type = FinanceEntryType.Expense, Order = 6 },
                new() { Name = Loc.Get("CatSubscriptions"), Icon = "\uD83D\uDCF1", Color = "#FB7185", Type = FinanceEntryType.Expense, Order = 7 },
                new() { Name = Loc.Get("CatEducation"), Icon = "\uD83D\uDCDA", Color = "#818CF8", Type = FinanceEntryType.Expense, Order = 8 },
                new() { Name = Loc.Get("CatOtherExpense"), Icon = "\u2796", Color = "#585878", Type = FinanceEntryType.Expense, Order = 9 },
            };

            db.FinanceCategories.AddRange(categories);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        // Update existing seed category names to current language
        var allCats = await db.FinanceCategories.ToListAsync(ct).ConfigureAwait(false);
        var changed = false;
        foreach (var cat in allCats)
        {
            // Check if name matches any known English seed name
            var key = SeedCategoryKeys.GetValueOrDefault(cat.Name);
            if (key is not null)
            {
                cat.Name = Loc.Get(key);
                changed = true;
            }
        }
        if (changed) await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task<List<FinanceEntry>> GetFinanceEntriesAsync(DateOnly from, DateOnly to, FinanceEntryType? type = null, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var query = db.FinanceEntries.Include(e => e.Category).Where(e => e.Date >= from && e.Date <= to);
        if (type is not null) query = query.Where(e => e.Type == type);
        return await query.OrderBy(e => e.Date).ThenBy(e => e.Id).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveFinanceEntryAsync(FinanceEntry entry, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();

        // Prevent entries on archived categories
        if (entry.CategoryId > 0)
        {
            var cat = await db.FinanceCategories.FindAsync([entry.CategoryId], ct).ConfigureAwait(false);
            if (cat is { IsArchived: true }) return;
        }

        // Detach navigation properties to avoid tracking conflicts
        var savedCategory = entry.Category;
        var savedWeek = entry.Week;
        var savedRecurring = entry.RecurringPayment;
        entry.Category = null;
        entry.Week = null;
        entry.RecurringPayment = null;

        if (entry.Id == 0)
            db.FinanceEntries.Add(entry);
        else
            db.FinanceEntries.Update(entry);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        entry.Category = savedCategory;
        entry.Week = savedWeek;
        entry.RecurringPayment = savedRecurring;
    }
    public async Task RemoveFinanceEntryAsync(int entryId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var entry = await db.FinanceEntries.FindAsync([entryId], ct).ConfigureAwait(false);
        if (entry is not null) { db.FinanceEntries.Remove(entry); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task<List<FinanceBudget>> GetBudgetsAsync(string monthYear, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        return await db.FinanceBudgets.Include(b => b.Category).Where(b => b.MonthYear == monthYear).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveBudgetAsync(FinanceBudget budget, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var savedCategory = budget.Category;
        budget.Category = null;

        if (budget.Id == 0)
            db.FinanceBudgets.Add(budget);
        else
            db.FinanceBudgets.Update(budget);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        budget.Category = savedCategory;
    }
    public async Task RemoveBudgetAsync(int budgetId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var b = await db.FinanceBudgets.FindAsync([budgetId], ct).ConfigureAwait(false);
        if (b is not null) { db.FinanceBudgets.Remove(b); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task<List<RecurringPayment>> GetRecurringPaymentsAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var query = db.RecurringPayments.Include(rp => rp.Category).AsQueryable();
        if (activeOnly) query = query.Where(rp => rp.IsActive);
        return await query.OrderBy(rp => rp.Type).ThenBy(rp => rp.Name).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveRecurringPaymentAsync(RecurringPayment payment, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var savedCategory = payment.Category;
        var savedEntries = payment.GeneratedEntries;
        payment.Category = null;
        payment.GeneratedEntries = [];

        if (payment.Id == 0)
            db.RecurringPayments.Add(payment);
        else
            db.RecurringPayments.Update(payment);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        payment.Category = savedCategory;
        payment.GeneratedEntries = savedEntries;
    }
    public async Task RemoveRecurringPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var rp = await db.RecurringPayments.FindAsync([paymentId], ct).ConfigureAwait(false);
        if (rp is not null) { db.RecurringPayments.Remove(rp); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task GenerateRecurringEntriesAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var payments = await db.RecurringPayments
            .Where(rp => rp.IsActive && rp.AutoCreate && rp.StartDate <= to && (rp.EndDate == null || rp.EndDate >= from))
            .ToListAsync(ct).ConfigureAwait(false);

        var existingKeys = (await db.FinanceEntries
            .Where(e => e.RecurringPaymentId != null && e.Date >= from && e.Date <= to)
            .Select(e => new { e.RecurringPaymentId, e.Date })
            .ToListAsync(ct).ConfigureAwait(false))
            .ToHashSet();

        var newEntries = new List<FinanceEntry>();

        foreach (var rp in payments)
        {
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                if (date < rp.StartDate || (rp.EndDate is not null && date > rp.EndDate)) continue;

                var match = rp.Frequency switch
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

                if (!match) continue;
                if (!existingKeys.Add(new { RecurringPaymentId = (int?)rp.Id, Date = date })) continue;

                newEntries.Add(new FinanceEntry
                {
                    Date = date,
                    CategoryId = rp.CategoryId,
                    Type = rp.Type,
                    Amount = rp.Amount,
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
        await using var db = PlannerDbContextFactory.Create();
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
        await using var db = PlannerDbContextFactory.Create();
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
        await using var db = PlannerDbContextFactory.Create();
        return await db.FinancialGoals.OrderBy(g => g.Order).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveFinancialGoalAsync(FinancialGoal goal, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (goal.Id == 0) db.FinancialGoals.Add(goal);
        else db.FinancialGoals.Update(goal);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveFinancialGoalAsync(int goalId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var goal = await db.FinancialGoals.FindAsync([goalId], ct).ConfigureAwait(false);
        if (goal is not null) { db.FinancialGoals.Remove(goal); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task<List<Account>> GetAccountsAsync(CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        return await db.Accounts.Where(a => !a.IsArchived).OrderBy(a => a.Order).ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveAccountAsync(Account account, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (account.Id == 0) db.Accounts.Add(account);
        else db.Accounts.Update(account);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveAccountAsync(int accountId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var account = await db.Accounts.FindAsync([accountId], ct).ConfigureAwait(false);
        if (account is not null) { db.Accounts.Remove(account); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task<decimal> GetAccountBalanceAsync(int accountId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
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
        await using var db = PlannerDbContextFactory.Create();
        return await db.AccountTransfers
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Where(t => t.Date >= from && t.Date <= to)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task SaveAccountTransferAsync(AccountTransfer transfer, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        if (transfer.Id == 0) db.AccountTransfers.Add(transfer);
        else db.AccountTransfers.Update(transfer);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    public async Task RemoveAccountTransferAsync(int transferId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        var transfer = await db.AccountTransfers.FindAsync([transferId], ct).ConfigureAwait(false);
        if (transfer is not null) { db.AccountTransfers.Remove(transfer); await db.SaveChangesAsync(ct).ConfigureAwait(false); }
    }
    public async Task<List<FinanceEntry>> GetSplitEntriesAsync(int parentEntryId, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
        return await db.FinanceEntries
            .Include(e => e.Category)
            .Where(e => e.ParentEntryId == parentEntryId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct).ConfigureAwait(false);
    }
    public async Task<List<ForecastDay>> GetBalanceForecastAsync(int days, CancellationToken ct = default)
    {
        await using var db = PlannerDbContextFactory.Create();
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

        await using var db = PlannerDbContextFactory.Create();
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
        await using var db = PlannerDbContextFactory.Create();
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
