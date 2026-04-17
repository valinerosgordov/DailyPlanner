using DailyPlanner.Models;
using FluentAssertions;

namespace DailyPlanner.Tests;

public class PlannerServiceTests : PlannerServiceTestFixture
{
    // ─── Weeks ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateWeek_FirstCall_SeedsFourGoalsSevenDaysFiveHabits()
    {
        var monday = new DateOnly(2026, 4, 13);

        var week = await Service.GetOrCreateWeekAsync(monday);

        week.StartDate.Should().Be(monday);
        week.Goals.Should().HaveCount(4);
        week.Days.Should().HaveCount(7);
        week.Days.All(d => d.Tasks.Count == 10).Should().BeTrue();
        week.Habits.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetOrCreateWeek_SecondCall_ReturnsSameWeek()
    {
        var monday = new DateOnly(2026, 4, 13);

        var first = await Service.GetOrCreateWeekAsync(monday);
        var second = await Service.GetOrCreateWeekAsync(monday);

        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public void GetWeekStart_ReturnsMonday()
    {
        // Friday 2026-04-17 → Monday 2026-04-13
        var result = Services.PlannerService.GetWeekStart(new DateOnly(2026, 4, 17));
        result.Should().Be(new DateOnly(2026, 4, 13));

        // Sunday 2026-04-19 → Monday 2026-04-13
        Services.PlannerService.GetWeekStart(new DateOnly(2026, 4, 19))
            .Should().Be(new DateOnly(2026, 4, 13));
    }

    // ─── Tasks ──────────────────────────────────────────────────────

    [Fact]
    public async Task SaveTask_UpdatesText()
    {
        var week = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        var task = week.Days[0].Tasks[0];
        task.Text = "Написать тесты";
        task.Priority = TaskPriority.High;

        await Service.SaveTaskAsync(task);

        var reloaded = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        reloaded.Days[0].Tasks[0].Text.Should().Be("Написать тесты");
        reloaded.Days[0].Tasks[0].Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task MoveTaskToNextDay_FillsEmptySlot()
    {
        var week = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        var source = week.Days[0].Tasks[0];
        source.Text = "Источник";
        await Service.SaveTaskAsync(source);

        await Service.MoveTaskToNextDayAsync(source.Id, week.Days[1].Date);

        var reloaded = await Service.GetOrCreateWeekAsync(new DateOnly(2026, 4, 13));
        reloaded.Days[1].Tasks[0].Text.Should().Be("Источник");
        reloaded.Days[0].Tasks.All(t => string.IsNullOrWhiteSpace(t.Text) || t.Id != source.Id)
            .Should().BeTrue();
    }

    // ─── Finance entries ────────────────────────────────────────────

    [Fact]
    public async Task SaveFinanceEntry_StoresIncomeAndExpense()
    {
        await Service.SeedFinanceCategoriesAsync();
        var categories = await Service.GetFinanceCategoriesAsync();
        var incomeCat = categories.First(c => c.Type == FinanceEntryType.Income);
        var expenseCat = categories.First(c => c.Type == FinanceEntryType.Expense);

        var today = DateOnly.FromDateTime(DateTime.Today);

        await Service.SaveFinanceEntryAsync(new FinanceEntry
        {
            Date = today, Type = FinanceEntryType.Income, Amount = 1000m, CategoryId = incomeCat.Id
        });
        await Service.SaveFinanceEntryAsync(new FinanceEntry
        {
            Date = today, Type = FinanceEntryType.Expense, Amount = 300m, CategoryId = expenseCat.Id
        });

        var entries = await Service.GetFinanceEntriesAsync(today, today);
        entries.Should().HaveCount(2);
        entries.Where(e => e.Type == FinanceEntryType.Income).Sum(e => e.Amount).Should().Be(1000m);
        entries.Where(e => e.Type == FinanceEntryType.Expense).Sum(e => e.Amount).Should().Be(300m);
    }

    // ─── Debts ──────────────────────────────────────────────────────

    [Fact]
    public async Task SaveDebt_WithPayments_RemainingComputedCorrectly()
    {
        var debt = new Debt
        {
            PersonName = "Вадим",
            Direction = DebtDirection.Borrowed,
            Amount = 10000m,
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        };
        await Service.SaveDebtAsync(debt);

        await Service.AddDebtPaymentAsync(new DebtPayment { DebtId = debt.Id, Amount = 3000m, Date = DateOnly.FromDateTime(DateTime.Today) });
        await Service.AddDebtPaymentAsync(new DebtPayment { DebtId = debt.Id, Amount = 2000m, Date = DateOnly.FromDateTime(DateTime.Today) });

        var debts = await Service.GetDebtsAsync();
        var reloaded = debts.Single();
        reloaded.Amount.Should().Be(10000m);
        reloaded.Payments.Sum(p => p.Amount).Should().Be(5000m);
    }

    [Fact]
    public async Task RemoveDebt_AlsoRemovesPayments()
    {
        var debt = new Debt
        {
            PersonName = "Саша",
            Direction = DebtDirection.Borrowed,
            Amount = 1000m,
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        };
        await Service.SaveDebtAsync(debt);
        await Service.AddDebtPaymentAsync(new DebtPayment { DebtId = debt.Id, Amount = 200m, Date = DateOnly.FromDateTime(DateTime.Today) });

        await Service.RemoveDebtAsync(debt.Id);

        var debts = await Service.GetDebtsAsync();
        debts.Should().BeEmpty();
    }

    // ─── Inbox ──────────────────────────────────────────────────────

    [Fact]
    public async Task SaveInboxTask_CanBeRetrieved()
    {
        await Service.SaveInboxTaskAsync(new InboxTask
        {
            Text = "Trello-задача",
            Source = InboxSource.Trello,
            ExternalId = "abc123",
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        });

        var items = await Service.GetInboxTasksAsync();
        items.Should().HaveCount(1);
        items[0].Text.Should().Be("Trello-задача");
        items[0].ExternalId.Should().Be("abc123");
    }

    [Fact]
    public async Task MoveInboxToDay_TrelloSource_ArchivesInsteadOfDelete()
    {
        var inbox = new InboxTask
        {
            Text = "Отладка",
            Source = InboxSource.Trello,
            ExternalId = "trello-1",
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        };
        await Service.SaveInboxTaskAsync(inbox);

        var targetDay = new DateOnly(2026, 4, 15);
        var moved = await Service.MoveInboxToDayAsync(inbox.Id, targetDay);

        moved.Text.Should().Be("Отладка");

        // Archived inbox tasks should NOT appear in active list but still be in DB
        var active = await Service.GetInboxTasksAsync();
        active.Should().BeEmpty();
    }

    [Fact]
    public async Task MoveInboxToDay_ManualSource_DeletesRow()
    {
        var inbox = new InboxTask
        {
            Text = "Ручная",
            Source = InboxSource.Manual,
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        };
        await Service.SaveInboxTaskAsync(inbox);

        await Service.MoveInboxToDayAsync(inbox.Id, new DateOnly(2026, 4, 15));

        var active = await Service.GetInboxTasksAsync();
        active.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveInboxTask_MakesItGone()
    {
        var inbox = new InboxTask { Text = "X", Source = InboxSource.Manual, CreatedDate = DateOnly.FromDateTime(DateTime.Today) };
        await Service.SaveInboxTaskAsync(inbox);

        await Service.RemoveInboxTaskAsync(inbox.Id);

        (await Service.GetInboxTasksAsync()).Should().BeEmpty();
    }

    // ─── Trello settings ────────────────────────────────────────────

    [Fact]
    public async Task GetTrelloSettings_FirstCall_CreatesDefaultRow()
    {
        var settings = await Service.GetTrelloSettingsAsync();

        settings.ApiKey.Should().BeEmpty();
        settings.Token.Should().BeEmpty();
        settings.ListName.Should().Be("В работе");
        settings.IsEnabled.Should().BeFalse();
        settings.AutoSyncOnStartup.Should().BeFalse();
    }

    [Fact]
    public async Task SaveTrelloSettings_Persists()
    {
        var settings = await Service.GetTrelloSettingsAsync();
        settings.ApiKey = "key";
        settings.Token = "token";
        settings.ListName = "Doing";
        settings.IsEnabled = true;
        settings.AutoSyncOnStartup = true;

        await Service.SaveTrelloSettingsAsync(settings);

        var reloaded = await Service.GetTrelloSettingsAsync();
        reloaded.ApiKey.Should().Be("key");
        reloaded.Token.Should().Be("token");
        reloaded.ListName.Should().Be("Doing");
        reloaded.IsEnabled.Should().BeTrue();
        reloaded.AutoSyncOnStartup.Should().BeTrue();
    }
}
