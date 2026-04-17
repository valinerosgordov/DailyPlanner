using DailyPlanner.Models;
using FluentAssertions;

namespace DailyPlanner.Tests;

public class FinanceIntegrationTests : PlannerServiceTestFixture
{
    private async Task<FinanceCategory> SeededExpense()
    {
        await Service.SeedFinanceCategoriesAsync();
        return (await Service.GetFinanceCategoriesAsync()).First(c => c.Type == FinanceEntryType.Expense);
    }

    private async Task<FinanceCategory> SeededIncome()
    {
        await Service.SeedFinanceCategoriesAsync();
        return (await Service.GetFinanceCategoriesAsync()).First(c => c.Type == FinanceEntryType.Income);
    }

    // ─── Categories ─────────────────────────────────────────────────

    [Fact]
    public async Task SeedCategories_CreatesDefaultSet()
    {
        await Service.SeedFinanceCategoriesAsync();
        var cats = await Service.GetFinanceCategoriesAsync();
        cats.Should().NotBeEmpty();
        cats.Should().Contain(c => c.Type == FinanceEntryType.Income);
        cats.Should().Contain(c => c.Type == FinanceEntryType.Expense);
    }

    [Fact]
    public async Task SeedCategories_IsIdempotent()
    {
        await Service.SeedFinanceCategoriesAsync();
        var count1 = (await Service.GetFinanceCategoriesAsync()).Count;
        await Service.SeedFinanceCategoriesAsync();
        var count2 = (await Service.GetFinanceCategoriesAsync()).Count;
        count2.Should().Be(count1);
    }

    [Fact]
    public async Task ArchiveCategory_HidesFromActiveList()
    {
        var cat = await SeededExpense();
        await Service.ArchiveFinanceCategoryAsync(cat.Id);
        var active = await Service.GetFinanceCategoriesAsync();
        active.Should().NotContain(c => c.Id == cat.Id);
    }

    [Fact]
    public async Task CanDeleteCategory_FalseIfUsedByEntries()
    {
        var cat = await SeededExpense();
        await Service.SaveFinanceEntryAsync(new FinanceEntry
        {
            CategoryId = cat.Id, Type = FinanceEntryType.Expense,
            Amount = 10m, Date = DateOnly.FromDateTime(DateTime.Today)
        });

        (await Service.CanDeleteFinanceCategoryAsync(cat.Id)).Should().BeFalse();
    }

    // ─── Budgets ────────────────────────────────────────────────────

    [Fact]
    public async Task SaveBudget_PersistsAndReturnsForMonth()
    {
        var cat = await SeededExpense();
        var ym = "2026-04";
        await Service.SaveBudgetAsync(new FinanceBudget { CategoryId = cat.Id, MonthYear = ym, Amount = 5000m });

        var budgets = await Service.GetBudgetsAsync(ym);
        budgets.Should().HaveCount(1);
        budgets[0].Amount.Should().Be(5000m);
    }

    [Fact]
    public async Task GetBudgets_DifferentMonth_ReturnsEmpty()
    {
        var cat = await SeededExpense();
        await Service.SaveBudgetAsync(new FinanceBudget { CategoryId = cat.Id, MonthYear = "2026-04", Amount = 1m });

        var budgets = await Service.GetBudgetsAsync("2026-05");
        budgets.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveBudget_DeletesRow()
    {
        var cat = await SeededExpense();
        var budget = new FinanceBudget { CategoryId = cat.Id, MonthYear = "2026-04", Amount = 1m };
        await Service.SaveBudgetAsync(budget);

        await Service.RemoveBudgetAsync(budget.Id);

        (await Service.GetBudgetsAsync("2026-04")).Should().BeEmpty();
    }

    // ─── Recurring payments ─────────────────────────────────────────

    [Fact]
    public async Task SaveRecurringPayment_StoresAndReturns()
    {
        var cat = await SeededExpense();
        var rp = new RecurringPayment
        {
            Name = "Интернет",
            CategoryId = cat.Id,
            Type = FinanceEntryType.Expense,
            Frequency = PaymentFrequency.Monthly,
            Amount = 500m,
            DayOfMonth = 15,
            StartDate = new DateOnly(2026, 1, 1),
            IsActive = true
        };
        await Service.SaveRecurringPaymentAsync(rp);

        var result = await Service.GetRecurringPaymentsAsync();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Интернет");
    }

    [Fact]
    public async Task GenerateRecurringEntries_MonthlyCreatesOnDayOfMonth()
    {
        var cat = await SeededExpense();
        await Service.SaveRecurringPaymentAsync(new RecurringPayment
        {
            Name = "Rent", CategoryId = cat.Id, Type = FinanceEntryType.Expense,
            Frequency = PaymentFrequency.Monthly, Amount = 3000m, DayOfMonth = 10,
            StartDate = new DateOnly(2026, 1, 1), IsActive = true, AutoCreate = true
        });

        await Service.GenerateRecurringEntriesAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        var entries = await Service.GetFinanceEntriesAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));
        entries.Should().ContainSingle();
        entries[0].Date.Should().Be(new DateOnly(2026, 4, 10));
        entries[0].Amount.Should().Be(3000m);
    }

    [Fact]
    public async Task GenerateRecurringEntries_Idempotent()
    {
        var cat = await SeededExpense();
        await Service.SaveRecurringPaymentAsync(new RecurringPayment
        {
            Name = "Rent", CategoryId = cat.Id, Type = FinanceEntryType.Expense,
            Frequency = PaymentFrequency.Monthly, Amount = 3000m, DayOfMonth = 10,
            StartDate = new DateOnly(2026, 1, 1), IsActive = true, AutoCreate = true
        });

        await Service.GenerateRecurringEntriesAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));
        await Service.GenerateRecurringEntriesAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        (await Service.GetFinanceEntriesAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30)))
            .Should().ContainSingle();
    }

    [Fact]
    public async Task GenerateRecurringEntries_InactiveSkipped()
    {
        var cat = await SeededExpense();
        await Service.SaveRecurringPaymentAsync(new RecurringPayment
        {
            Name = "Old", CategoryId = cat.Id, Type = FinanceEntryType.Expense,
            Frequency = PaymentFrequency.Monthly, Amount = 1m, DayOfMonth = 10,
            StartDate = new DateOnly(2026, 1, 1), IsActive = false, AutoCreate = true
        });

        await Service.GenerateRecurringEntriesAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        (await Service.GetFinanceEntriesAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30)))
            .Should().BeEmpty();
    }

    // ─── Accounts + transfers ───────────────────────────────────────

    [Fact]
    public async Task AccountBalance_InitialBalancePlusIncomeMinusExpense()
    {
        var incomeCat = await SeededIncome();
        var account = new Account { Name = "Main", InitialBalance = 1000m };
        await Service.SaveAccountAsync(account);

        await Service.SaveFinanceEntryAsync(new FinanceEntry
        {
            AccountId = account.Id, CategoryId = incomeCat.Id,
            Type = FinanceEntryType.Income, Amount = 500m,
            Date = DateOnly.FromDateTime(DateTime.Today)
        });
        var expenseCat = (await Service.GetFinanceCategoriesAsync()).First(c => c.Type == FinanceEntryType.Expense);
        await Service.SaveFinanceEntryAsync(new FinanceEntry
        {
            AccountId = account.Id, CategoryId = expenseCat.Id,
            Type = FinanceEntryType.Expense, Amount = 200m,
            Date = DateOnly.FromDateTime(DateTime.Today)
        });

        var balance = await Service.GetAccountBalanceAsync(account.Id);
        balance.Should().Be(1300m); // 1000 + 500 - 200
    }

    [Fact]
    public async Task AccountBalance_TransferDebitsFromCreditsTo()
    {
        var from = new Account { Name = "Debit", InitialBalance = 1000m };
        var to = new Account { Name = "Savings", InitialBalance = 0m };
        await Service.SaveAccountAsync(from);
        await Service.SaveAccountAsync(to);

        await Service.SaveAccountTransferAsync(new AccountTransfer
        {
            FromAccountId = from.Id, ToAccountId = to.Id, Amount = 300m,
            Date = DateOnly.FromDateTime(DateTime.Today)
        });

        (await Service.GetAccountBalanceAsync(from.Id)).Should().Be(700m);
        (await Service.GetAccountBalanceAsync(to.Id)).Should().Be(300m);
    }

    // ─── Analytics ──────────────────────────────────────────────────

    [Fact]
    public async Task GetExpensesByCategory_GroupsAndSums()
    {
        var expense = await SeededExpense();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await Service.SaveFinanceEntryAsync(new FinanceEntry { CategoryId = expense.Id, Type = FinanceEntryType.Expense, Amount = 100m, Date = today });
        await Service.SaveFinanceEntryAsync(new FinanceEntry { CategoryId = expense.Id, Type = FinanceEntryType.Expense, Amount = 200m, Date = today });

        var breakdown = await Service.GetExpensesByCategoryAsync(today, today);
        breakdown.Should().ContainSingle();
        breakdown[0].Amount.Should().Be(300m);
    }

    [Fact]
    public async Task GetMonthlyTotals_ReturnsEntryPerMonth()
    {
        var inc = await SeededIncome();
        var exp = (await Service.GetFinanceCategoriesAsync()).First(c => c.Type == FinanceEntryType.Expense);

        var thisMonth = DateOnly.FromDateTime(DateTime.Today);
        await Service.SaveFinanceEntryAsync(new FinanceEntry { CategoryId = inc.Id, Type = FinanceEntryType.Income, Amount = 1000m, Date = thisMonth });
        await Service.SaveFinanceEntryAsync(new FinanceEntry { CategoryId = exp.Id, Type = FinanceEntryType.Expense, Amount = 300m, Date = thisMonth });

        var totals = await Service.GetMonthlyTotalsAsync(3);
        totals.Should().NotBeEmpty();
        var current = totals.First(t => t.Year == thisMonth.Year && t.Month == thisMonth.Month);
        current.Income.Should().Be(1000m);
        current.Expenses.Should().Be(300m);
    }

    // ─── Split entries ──────────────────────────────────────────────

    [Fact]
    public async Task SplitEntries_ParentHasChildren()
    {
        var exp = await SeededExpense();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var parent = new FinanceEntry
        {
            CategoryId = exp.Id, Type = FinanceEntryType.Expense, Amount = 1000m, Date = today,
            Description = "Grocery trip"
        };
        await Service.SaveFinanceEntryAsync(parent);

        await Service.SaveFinanceEntryAsync(new FinanceEntry
        {
            CategoryId = exp.Id, Type = FinanceEntryType.Expense, Amount = 600m, Date = today,
            ParentEntryId = parent.Id, Description = "Milk"
        });
        await Service.SaveFinanceEntryAsync(new FinanceEntry
        {
            CategoryId = exp.Id, Type = FinanceEntryType.Expense, Amount = 400m, Date = today,
            ParentEntryId = parent.Id, Description = "Bread"
        });

        var splits = await Service.GetSplitEntriesAsync(parent.Id);
        splits.Should().HaveCount(2);
        splits.Sum(s => s.Amount).Should().Be(1000m);
    }

    // ─── Financial goals ────────────────────────────────────────────

    [Fact]
    public async Task FinancialGoal_SaveRemoveRoundTrip()
    {
        await Service.SaveFinancialGoalAsync(new FinancialGoal
        {
            Name = "New laptop", TargetAmount = 100000m, SavedAmount = 20000m,
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        });

        var list = await Service.GetFinancialGoalsAsync();
        list.Should().ContainSingle(g => g.Name == "New laptop");

        await Service.RemoveFinancialGoalAsync(list[0].Id);
        (await Service.GetFinancialGoalsAsync()).Should().BeEmpty();
    }
}
