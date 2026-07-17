using DailyPlanner.Models;
using FluentAssertions;

namespace DailyPlanner.Tests;

/// <summary>
/// "Settled" derives from payments: a fully paid debt closes itself, removing a
/// payment below the full amount reopens it. IsSettled used to be an unrelated
/// manual flag — fully paid debts stayed in the active (and overdue) list.
/// </summary>
public class DebtSettleTests : PlannerServiceTestFixture
{
    private async Task<Debt> CreateDebtAsync(decimal amount)
    {
        var debt = new Debt
        {
            PersonName = "Иван",
            Direction = DebtDirection.Lent,
            Amount = amount,
            CreatedDate = DateOnly.FromDateTime(DateTime.Today)
        };
        await Service.SaveDebtAsync(debt);
        return debt;
    }

    [Fact]
    public async Task AddPayment_CoveringFullAmount_AutoSettlesTheDebt()
    {
        var debt = await CreateDebtAsync(1000m);

        await Service.AddDebtPaymentAsync(new DebtPayment
        {
            DebtId = debt.Id,
            Amount = 1000m,
            Date = DateOnly.FromDateTime(DateTime.Today)
        });

        var settled = (await Service.GetDebtsAsync(includeSettled: true)).Single(d => d.Id == debt.Id);
        settled.IsSettled.Should().BeTrue("a fully paid debt must not hang in the active list");
        settled.SettledDate.Should().NotBeNull();
    }

    [Fact]
    public async Task AddPayment_Partial_KeepsDebtActive()
    {
        var debt = await CreateDebtAsync(1000m);

        await Service.AddDebtPaymentAsync(new DebtPayment
        {
            DebtId = debt.Id,
            Amount = 400m,
            Date = DateOnly.FromDateTime(DateTime.Today)
        });

        var active = (await Service.GetDebtsAsync()).Single(d => d.Id == debt.Id);
        active.IsSettled.Should().BeFalse();
    }

    [Fact]
    public async Task RemovePayment_DroppingBelowFullAmount_ReopensTheDebt()
    {
        var debt = await CreateDebtAsync(1000m);
        var payment = new DebtPayment
        {
            DebtId = debt.Id,
            Amount = 1000m,
            Date = DateOnly.FromDateTime(DateTime.Today)
        };
        await Service.AddDebtPaymentAsync(payment);

        await Service.RemoveDebtPaymentAsync(payment.Id);

        var reopened = (await Service.GetDebtsAsync()).Single(d => d.Id == debt.Id);
        reopened.IsSettled.Should().BeFalse("payments no longer cover the amount");
        reopened.SettledDate.Should().BeNull();
    }
}
