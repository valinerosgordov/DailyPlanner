using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class FinanceViewModel : ObservableObject
{
    private readonly PlannerService _service;

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private int _selectedYear = DateTime.Today.Year;
    [ObservableProperty] private int _selectedMonth = DateTime.Today.Month;
    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private bool _isLoading;

    // Summary
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpenses;
    [ObservableProperty] private decimal _balance;
    [ObservableProperty] private decimal _debtOwedToMe;
    [ObservableProperty] private decimal _debtIOwn;
    [ObservableProperty] private decimal _monthlyObligatory;
    [ObservableProperty] private decimal _savings;
    [ObservableProperty] private string _savingsTrendArrow = string.Empty;
    [ObservableProperty] private double _savingsRatePercent;

    public ObservableCollection<FinanceEntryViewModel> IncomeEntries { get; } = [];
    public ObservableCollection<FinanceEntryViewModel> ExpenseEntries { get; } = [];
    public ObservableCollection<FinanceCategoryViewModel> IncomeCategories { get; } = [];
    public ObservableCollection<FinanceCategoryViewModel> ExpenseCategories { get; } = [];
    public ObservableCollection<BudgetViewModel> Budgets { get; } = [];
    public ObservableCollection<DebtViewModel> LentDebts { get; } = [];
    public ObservableCollection<DebtViewModel> BorrowedDebts { get; } = [];
    public ObservableCollection<RecurringPaymentViewModel> RecurringPayments { get; } = [];
    public ObservableCollection<CategoryBreakdownItem> CategoryBreakdown { get; } = [];
    public ObservableCollection<MonthlyFinanceSummary> MonthlyTrend { get; } = [];

    // Empty state flags
    public bool HasIncomeEntries => IncomeEntries.Count > 0;
    public bool HasExpenseEntries => ExpenseEntries.Count > 0;
    public bool HasBudgets => Budgets.Count > 0;
    public bool HasLentDebts => LentDebts.Count > 0;
    public bool HasBorrowedDebts => BorrowedDebts.Count > 0;
    public bool HasRecurringPayments => RecurringPayments.Count > 0;
    public bool HasCategoryBreakdown => CategoryBreakdown.Count > 0;
    public bool HasMonthlyTrend => MonthlyTrend.Count > 0;
    public int MonthlyTrendCount => MonthlyTrend.Count;

    public FinanceViewModel(PlannerService service)
    {
        _service = service;
        IncomeEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasIncomeEntries));
        ExpenseEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasExpenseEntries));
        Budgets.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasBudgets));
        LentDebts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasLentDebts));
        BorrowedDebts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasBorrowedDebts));
        RecurringPayments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRecurringPayments));
        CategoryBreakdown.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCategoryBreakdown));
        MonthlyTrend.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasMonthlyTrend));
            OnPropertyChanged(nameof(MonthlyTrendCount));
        };
    }

    private readonly SemaphoreSlim _loadGate = new(1, 1);

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (!_loadGate.Wait(0)) return;
        IsLoading = true;
        try
        {
        await _service.SeedFinanceCategoriesAsync();

        PeriodLabel = $"{Loc.GetMonthName(SelectedMonth)} {SelectedYear}";

        var firstDay = new DateOnly(SelectedYear, SelectedMonth, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var monthYear = $"{SelectedYear:D4}-{SelectedMonth:D2}";

        // Generate recurring entries for this month
        await _service.GenerateRecurringEntriesAsync(firstDay, lastDay);

        // Load categories
        await LoadCategoriesAsync();

        // Load entries
        var entries = await _service.GetFinanceEntriesAsync(firstDay, lastDay);

        IncomeEntries.Clear();
        ExpenseEntries.Clear();
        decimal income = 0, expenses = 0;

        foreach (var entry in entries)
        {
            var vm = new FinanceEntryViewModel(entry, _service);
            if (entry.Type == FinanceEntryType.Income)
            {
                IncomeEntries.Add(vm);
                income += entry.Amount;
            }
            else
            {
                ExpenseEntries.Add(vm);
                expenses += entry.Amount;
            }
        }

        TotalIncome = income;
        TotalExpenses = expenses;
        Balance = income - expenses;

        // Load budgets
        var budgets = await _service.GetBudgetsAsync(monthYear);
        Budgets.Clear();
        foreach (var b in budgets)
        {
            var vm = new BudgetViewModel(b, _service);
            var spent = entries.Where(e => e.Type == FinanceEntryType.Expense && e.CategoryId == b.CategoryId)
                .Sum(e => e.Amount);
            vm.SpentAmount = spent;
            Budgets.Add(vm);
        }

        // Load debts
        var debts = await _service.GetDebtsAsync();
        LentDebts.Clear();
        BorrowedDebts.Clear();
        decimal owedToMe = 0, iOwe = 0;

        foreach (var d in debts)
        {
            var vm = new DebtViewModel(d, _service);
            if (d.Direction == DebtDirection.Lent)
            {
                LentDebts.Add(vm);
                owedToMe += vm.RemainingAmount;
            }
            else
            {
                BorrowedDebts.Add(vm);
                iOwe += vm.RemainingAmount;
            }
        }

        DebtOwedToMe = owedToMe;
        DebtIOwn = iOwe;

        // Load recurring payments
        var recurring = await _service.GetRecurringPaymentsAsync();
        RecurringPayments.Clear();
        decimal obligatory = 0;
        foreach (var rp in recurring)
        {
            RecurringPayments.Add(new RecurringPaymentViewModel(rp, _service));
            if (rp.Type == FinanceEntryType.Expense)
                obligatory += rp.Frequency switch
                {
                    PaymentFrequency.Monthly => rp.Amount,
                    PaymentFrequency.Weekly => rp.Amount * 4.33m,
                    PaymentFrequency.Biweekly => rp.Amount * 2.17m,
                    PaymentFrequency.Quarterly => rp.Amount / 3,
                    PaymentFrequency.Yearly => rp.Amount / 12,
                    _ => 0
                };
        }
        MonthlyObligatory = Math.Round(obligatory, 2);

        // Savings
        Savings = income - expenses;
        SavingsRatePercent = income > 0 ? Math.Max(0, Math.Round((double)(Savings / income) * 100, 1)) : 0;

        // Analytics: category breakdown
        var breakdown = await _service.GetExpensesByCategoryAsync(firstDay, lastDay);
        CategoryBreakdown.Clear();
        foreach (var item in breakdown)
            CategoryBreakdown.Add(item);

        // Analytics: 6-month trend
        var trend = await _service.GetMonthlyTotalsAsync(6);
        MonthlyTrend.Clear();
        foreach (var item in trend)
            MonthlyTrend.Add(item);

        // Savings trend arrow (compare to previous month)
        if (trend.Count >= 2)
        {
            var prev = trend[^2].Balance;
            SavingsTrendArrow = Savings > prev ? "↑" : Savings < prev ? "↓" : "→";
        }
        else
        {
            SavingsTrendArrow = string.Empty;
        }

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinanceVM] LoadData failed: {ex}");
        }
        finally
        {
            IsLoading = false;
            _loadGate.Release();
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var allCategories = await _service.GetFinanceCategoriesAsync();

        IncomeCategories.Clear();
        ExpenseCategories.Clear();
        foreach (var c in allCategories)
        {
            var vm = new FinanceCategoryViewModel(c, _service);
            if (c.Type == FinanceEntryType.Income)
                IncomeCategories.Add(vm);
            else
                ExpenseCategories.Add(vm);
        }
    }

    [RelayCommand]
    private async Task AddIncomeEntryAsync()
    {
        if (IncomeCategories.Count == 0) return;
        var cat = IncomeCategories[0];
        var entry = new FinanceEntry
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Type = FinanceEntryType.Income,
            CategoryId = cat.Id,
            IsPaid = true
        };
        await _service.SaveFinanceEntryAsync(entry);
        entry.Category = cat.Model;
        IncomeEntries.Add(new FinanceEntryViewModel(entry, _service));
    }

    [RelayCommand]
    private async Task AddExpenseEntryAsync()
    {
        if (ExpenseCategories.Count == 0) return;
        var cat = ExpenseCategories[0];
        var entry = new FinanceEntry
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Type = FinanceEntryType.Expense,
            CategoryId = cat.Id,
            IsPaid = true
        };
        await _service.SaveFinanceEntryAsync(entry);
        entry.Category = cat.Model;
        ExpenseEntries.Add(new FinanceEntryViewModel(entry, _service));
    }

    [RelayCommand]
    private async Task RemoveIncomeEntryAsync(FinanceEntryViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveFinanceEntryAsync(vm.Model.Id);
        IncomeEntries.Remove(vm);
        TotalIncome -= vm.Amount;
        Balance = TotalIncome - TotalExpenses;
    }

    [RelayCommand]
    private async Task RemoveExpenseEntryAsync(FinanceEntryViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveFinanceEntryAsync(vm.Model.Id);
        ExpenseEntries.Remove(vm);
        TotalExpenses -= vm.Amount;
        Balance = TotalIncome - TotalExpenses;
    }

    [RelayCommand]
    private async Task AddDebtAsync(string? directionStr)
    {
        var direction = directionStr == "Borrowed" ? DebtDirection.Borrowed : DebtDirection.Lent;
        var debt = new Debt
        {
            Direction = direction,
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        };
        await _service.SaveDebtAsync(debt);
        var vm = new DebtViewModel(debt, _service);
        if (direction == DebtDirection.Lent)
            LentDebts.Add(vm);
        else
            BorrowedDebts.Add(vm);
    }

    [RelayCommand]
    private async Task RemoveDebtAsync(DebtViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveDebtAsync(vm.Model.Id);
        LentDebts.Remove(vm);
        BorrowedDebts.Remove(vm);
    }

    [RelayCommand]
    private async Task AddDebtPaymentAsync(DebtViewModel? debtVm)
    {
        if (debtVm is null || debtVm.RemainingAmount <= 0) return;
        var payment = new DebtPayment
        {
            DebtId = debtVm.Model.Id,
            Date = DateOnly.FromDateTime(DateTime.Today)
        };
        await _service.AddDebtPaymentAsync(payment);
        debtVm.AddPayment(payment);
        RefreshDebtSummary();
    }

    private void RefreshDebtSummary()
    {
        DebtOwedToMe = LentDebts.Sum(d => d.RemainingAmount);
        DebtIOwn = BorrowedDebts.Sum(d => d.RemainingAmount);
    }

    [RelayCommand]
    private async Task AddRecurringPaymentAsync(string? typeStr)
    {
        var type = typeStr == "Income" ? FinanceEntryType.Income : FinanceEntryType.Expense;
        var categories = await _service.GetFinanceCategoriesAsync(type);
        if (categories.Count == 0) return;
        var cat = categories[0];
        var rp = new RecurringPayment
        {
            Type = type,
            CategoryId = cat.Id,
            Frequency = PaymentFrequency.Monthly,
            DayOfMonth = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            IsActive = true,
            AutoCreate = true
        };
        await _service.SaveRecurringPaymentAsync(rp);
        rp.Category = cat;
        RecurringPayments.Add(new RecurringPaymentViewModel(rp, _service));
    }

    [RelayCommand]
    private async Task RemoveRecurringPaymentAsync(RecurringPaymentViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveRecurringPaymentAsync(vm.Model.Id);
        RecurringPayments.Remove(vm);
    }

    [RelayCommand]
    private async Task AddBudgetAsync()
    {
        try
        {
            if (ExpenseCategories.Count == 0) return;
            var monthYear = $"{SelectedYear:D4}-{SelectedMonth:D2}";
            var usedCategoryIds = Budgets.Select(b => b.Model.CategoryId).ToHashSet();
            var cat = ExpenseCategories.FirstOrDefault(c => !usedCategoryIds.Contains(c.Id));
            if (cat is null) return;

            var budget = new FinanceBudget
            {
                CategoryId = cat.Id,
                MonthYear = monthYear,
                Amount = 0
            };
            await _service.SaveBudgetAsync(budget);
            budget.Category = cat.Model;
            Budgets.Add(new BudgetViewModel(budget, _service));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinanceVM] AddBudget failed: {ex}");
        }
    }

    [RelayCommand]
    private async Task RemoveBudgetAsync(BudgetViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveBudgetAsync(vm.Model.Id);
        Budgets.Remove(vm);
    }

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        if (SelectedMonth == 1) { SelectedMonth = 12; SelectedYear--; }
        else SelectedMonth--;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        if (SelectedMonth == 12) { SelectedMonth = 1; SelectedYear++; }
        else SelectedMonth++;
        await LoadDataAsync();
    }

    [RelayCommand]
    private void ExportFinance()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"Finance_{SelectedYear}-{SelectedMonth:D2}",
            DefaultExt = ".xlsx",
            Filter = "Excel (.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            var firstDay = new DateOnly(SelectedYear, SelectedMonth, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            var success = ExportService.ExportFinanceToExcel(
                PeriodLabel, IncomeEntries, ExpenseEntries, Budgets,
                CategoryBreakdown, TotalIncome, TotalExpenses, Balance, dialog.FileName);

            if (success)
                System.Windows.MessageBox.Show(Loc.Get("ExportSuccess"), Loc.Get("ExportTitle"));
            else
                System.Windows.MessageBox.Show(Loc.Get("ExportError"), Loc.Get("ExportTitle"));
        }
    }
}
