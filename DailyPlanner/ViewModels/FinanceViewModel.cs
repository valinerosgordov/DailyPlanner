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
    [ObservableProperty] private decimal _netWorth;

    // Filter / Search
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int? _filterCategoryId;
    [ObservableProperty] private DateOnly? _filterDateFrom;
    [ObservableProperty] private DateOnly? _filterDateTo;

    // Income Sources
    [ObservableProperty] private decimal _expectedIncome;
    [ObservableProperty] private decimal _receivedIncome;

    // Forecast
    [ObservableProperty] private decimal _forecastBalance;
    [ObservableProperty] private decimal _projectedIncome;
    [ObservableProperty] private decimal _projectedExpenses;

    public ObservableCollection<FinanceEntryViewModel> IncomeEntries { get; } = [];
    public ObservableCollection<FinanceEntryViewModel> ExpenseEntries { get; } = [];
    public ObservableCollection<FinanceEntryViewModel> FilteredIncomeEntries { get; } = [];
    public ObservableCollection<FinanceEntryViewModel> FilteredExpenseEntries { get; } = [];
    public ObservableCollection<FinanceCategoryViewModel> IncomeCategories { get; } = [];
    public ObservableCollection<FinanceCategoryViewModel> ExpenseCategories { get; } = [];
    public ObservableCollection<BudgetViewModel> Budgets { get; } = [];
    public ObservableCollection<DebtViewModel> LentDebts { get; } = [];
    public ObservableCollection<DebtViewModel> BorrowedDebts { get; } = [];
    public ObservableCollection<RecurringPaymentViewModel> RecurringPayments { get; } = [];
    public ObservableCollection<CategoryBreakdownItem> CategoryBreakdown { get; } = [];
    public ObservableCollection<MonthlyFinanceSummary> MonthlyTrend { get; } = [];
    public ObservableCollection<FinancialGoalViewModel> FinancialGoals { get; } = [];
    public ObservableCollection<AccountViewModel> Accounts { get; } = [];
    public ObservableCollection<AccountTransferViewModel> Transfers { get; } = [];
    public ObservableCollection<IncomeSourceViewModel> IncomeSources { get; } = [];
    public ObservableCollection<IncomeSourceStatus> IncomeSourceStatuses { get; } = [];
    public ObservableCollection<ForecastDay> ForecastDays { get; } = [];
    public ObservableCollection<CashflowDay> CashflowDays { get; } = [];

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
    public bool HasFinancialGoals => FinancialGoals.Count > 0;
    public bool HasAccounts => Accounts.Count > 0;
    public bool HasTransfers => Transfers.Count > 0;
    public bool HasIncomeSources => IncomeSources.Count > 0;
    public bool HasIncomeSourceStatuses => IncomeSourceStatuses.Count > 0;
    public bool HasForecast => ForecastDays.Count > 0;
    public bool HasCashflow => CashflowDays.Count > 0;
    public bool HasFilteredIncome => FilteredIncomeEntries.Count > 0;
    public bool HasFilteredExpense => FilteredExpenseEntries.Count > 0;
    public bool IsFilterActive => !string.IsNullOrEmpty(SearchText) || FilterCategoryId is not null
        || FilterDateFrom is not null || FilterDateTo is not null;

    public FinanceViewModel(PlannerService service)
    {
        _service = service;
        IncomeEntries.CollectionChanged += (_, _) => { OnPropertyChanged(nameof(HasIncomeEntries)); ApplyFilter(); };
        ExpenseEntries.CollectionChanged += (_, _) => { OnPropertyChanged(nameof(HasExpenseEntries)); ApplyFilter(); };
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
        FinancialGoals.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFinancialGoals));
        Accounts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAccounts));
        Transfers.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTransfers));
        IncomeSources.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasIncomeSources));
        IncomeSourceStatuses.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasIncomeSourceStatuses));
        ForecastDays.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasForecast));
        CashflowDays.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCashflow));
        FilteredIncomeEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFilteredIncome));
        FilteredExpenseEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFilteredExpense));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnFilterCategoryIdChanged(int? value) => ApplyFilter();
    partial void OnFilterDateFromChanged(DateOnly? value) => ApplyFilter();
    partial void OnFilterDateToChanged(DateOnly? value) => ApplyFilter();

    private void ApplyFilter()
    {
        FilteredIncomeEntries.Clear();
        FilteredExpenseEntries.Clear();

        foreach (var e in IncomeEntries)
            if (MatchesFilter(e)) FilteredIncomeEntries.Add(e);
        foreach (var e in ExpenseEntries)
            if (MatchesFilter(e)) FilteredExpenseEntries.Add(e);

        OnPropertyChanged(nameof(IsFilterActive));
    }

    private bool MatchesFilter(FinanceEntryViewModel entry)
    {
        if (!string.IsNullOrEmpty(SearchText) &&
            !entry.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
            !entry.CategoryName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            return false;
        if (FilterCategoryId is not null && entry.CategoryId != FilterCategoryId)
            return false;
        if (FilterDateFrom is not null && entry.Date < FilterDateFrom)
            return false;
        if (FilterDateTo is not null && entry.Date > FilterDateTo)
            return false;
        return true;
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SearchText = string.Empty;
        FilterCategoryId = null;
        FilterDateFrom = null;
        FilterDateTo = null;
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

        // Load entries (exclude split children — they're shown under parents)
        var entries = await _service.GetFinanceEntriesAsync(firstDay, lastDay);

        IncomeEntries.Clear();
        ExpenseEntries.Clear();
        decimal income = 0, expenses = 0;

        foreach (var entry in entries.Where(e => e.ParentEntryId is null))
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
        foreach (var rp in recurring)
            RecurringPayments.Add(new RecurringPaymentViewModel(rp, _service));
        MonthlyObligatory = FinanceCalculations.MonthlyObligatory(recurring);

        // Savings & Net Worth
        Savings = income - expenses;
        SavingsRatePercent = FinanceCalculations.SavingsRatePercent(Savings, income);
        NetWorth = FinanceCalculations.NetWorth(Balance, owedToMe, iOwe);

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

        // Financial Goals
        var goals = await _service.GetFinancialGoalsAsync();
        FinancialGoals.Clear();
        foreach (var g in goals)
            FinancialGoals.Add(new FinancialGoalViewModel(g, _service));

        // Accounts
        var accounts = await _service.GetAccountsAsync();
        Accounts.Clear();
        foreach (var a in accounts)
        {
            var avm = new AccountViewModel(a, _service);
            avm.CurrentBalance = await _service.GetAccountBalanceAsync(a.Id);
            Accounts.Add(avm);
        }

        // Account transfers for current month
        var transfers = await _service.GetAccountTransfersAsync(firstDay, lastDay);
        Transfers.Clear();
        foreach (var t in transfers)
            Transfers.Add(new AccountTransferViewModel(t, _service));

        // Income Sources
        var incomeSources = await _service.GetIncomeSourcesAsync();
        IncomeSources.Clear();
        foreach (var s in incomeSources)
            IncomeSources.Add(new IncomeSourceViewModel(s, _service));

        var statuses = await _service.GetIncomeSourceStatusAsync(SelectedYear, SelectedMonth);
        IncomeSourceStatuses.Clear();
        decimal expInc = 0, recvInc = 0;
        foreach (var st in statuses)
        {
            IncomeSourceStatuses.Add(st);
            expInc += st.Expected;
            recvInc += st.Received;
        }
        ExpectedIncome = expInc;
        ReceivedIncome = recvInc;

        // Forecast (30 days)
        var forecast = await _service.GetBalanceForecastAsync(30);
        ForecastDays.Clear();
        foreach (var fd in forecast)
            ForecastDays.Add(fd);
        if (forecast.Count > 0)
        {
            var last = forecast[^1];
            ForecastBalance = last.Balance;
            ProjectedIncome = forecast.Sum(f => f.Income);
            ProjectedExpenses = forecast.Sum(f => f.Expenses);
        }

        // Cashflow calendar
        var cashflow = await _service.GetCashflowCalendarAsync(SelectedYear, SelectedMonth);
        CashflowDays.Clear();
        foreach (var cd in cashflow)
            CashflowDays.Add(cd);

        }
        catch (Exception ex)
        {
            Log.Error("FinanceVM", $"LoadData failed: {ex}");
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

    // ─── Entry CRUD ──────────────────────────────────────────────

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

    // ─── Split Entries ───────────────────────────────────────────

    [RelayCommand]
    private async Task AddSplitEntryAsync(FinanceEntryViewModel? parentVm)
    {
        if (parentVm is null) return;
        var categories = parentVm.Type == FinanceEntryType.Income ? IncomeCategories : ExpenseCategories;
        if (categories.Count == 0) return;

        var split = new FinanceEntry
        {
            Date = parentVm.Date,
            Type = parentVm.Type,
            CategoryId = categories[0].Id,
            ParentEntryId = parentVm.Model.Id,
            IsPaid = true
        };
        await _service.SaveFinanceEntryAsync(split);
        split.Category = categories[0].Model;
        parentVm.SplitEntries.Add(new FinanceEntryViewModel(split, _service));
    }

    // ─── Debts ───────────────────────────────────────────────────

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
        NetWorth = FinanceCalculations.NetWorth(Balance, DebtOwedToMe, DebtIOwn);
    }

    // ─── Recurring Payments ──────────────────────────────────────

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

    // ─── Budgets ─────────────────────────────────────────────────

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
            Log.Error("FinanceVM", $"AddBudget failed: {ex}");
        }
    }

    [RelayCommand]
    private async Task RemoveBudgetAsync(BudgetViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveBudgetAsync(vm.Model.Id);
        Budgets.Remove(vm);
    }

    // ─── Financial Goals ─────────────────────────────────────────

    [RelayCommand]
    private async Task AddFinancialGoalAsync()
    {
        var goal = new FinancialGoal
        {
            Order = FinancialGoals.Count + 1,
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        };
        await _service.SaveFinancialGoalAsync(goal);
        FinancialGoals.Add(new FinancialGoalViewModel(goal, _service));
    }

    [RelayCommand]
    private async Task RemoveFinancialGoalAsync(FinancialGoalViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveFinancialGoalAsync(vm.Model.Id);
        FinancialGoals.Remove(vm);
    }

    // ─── Accounts ────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        var account = new Account { Order = Accounts.Count + 1 };
        await _service.SaveAccountAsync(account);
        Accounts.Add(new AccountViewModel(account, _service));
    }

    [RelayCommand]
    private async Task RemoveAccountAsync(AccountViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveAccountAsync(vm.Model.Id);
        Accounts.Remove(vm);
    }

    [RelayCommand]
    private async Task AddTransferAsync()
    {
        if (Accounts.Count < 2) return;
        var transfer = new AccountTransfer
        {
            FromAccountId = Accounts[0].Id,
            ToAccountId = Accounts[1].Id,
            Date = DateOnly.FromDateTime(DateTime.Today)
        };
        await _service.SaveAccountTransferAsync(transfer);
        transfer.FromAccount = Accounts[0].Model;
        transfer.ToAccount = Accounts[1].Model;
        Transfers.Insert(0, new AccountTransferViewModel(transfer, _service));
    }

    [RelayCommand]
    private async Task RemoveTransferAsync(AccountTransferViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveAccountTransferAsync(vm.Model.Id);
        Transfers.Remove(vm);
    }

    // ─── Income Sources ─────────────────────────────────────────

    [RelayCommand]
    private async Task AddIncomeSourceAsync()
    {
        var source = new IncomeSource
        {
            Order = IncomeSources.Count + 1
        };
        await _service.SaveIncomeSourceAsync(source);
        IncomeSources.Add(new IncomeSourceViewModel(source, _service));
    }

    [RelayCommand]
    private async Task RemoveIncomeSourceAsync(IncomeSourceViewModel? vm)
    {
        if (vm is null) return;
        await _service.RemoveIncomeSourceAsync(vm.Model.Id);
        IncomeSources.Remove(vm);
    }

    // ─── Import ──────────────────────────────────────────────────

    [RelayCommand]
    private async Task ImportFromFileAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = $"{Loc.Get("ImportCSV")}|{Loc.Get("ImportExcel")}",
            Title = Loc.Get("ImportFile")
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var count = await _service.ImportFinanceEntriesFromCsvAsync(dialog.FileName);
            NotificationService.ShowToast(Loc.Get("ImportFile"), string.Format(Loc.Get("ImportSuccess"), count));
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            Log.Error("FinanceVM", $"Import failed: {ex}");
            NotificationService.ShowToast(Loc.Get("ImportFile"), Loc.Get("ImportError"));
        }
    }

    // ─── Navigation ──────────────────────────────────────────────

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
            var success = ExportService.ExportFinanceToExcel(
                PeriodLabel, IncomeEntries, ExpenseEntries, Budgets,
                CategoryBreakdown, TotalIncome, TotalExpenses, Balance, dialog.FileName);

            NotificationService.ShowToast(Loc.Get("ExportTitle"),
                success ? Loc.Get("ExportSuccess") : Loc.Get("ExportError"));
        }
    }
}
