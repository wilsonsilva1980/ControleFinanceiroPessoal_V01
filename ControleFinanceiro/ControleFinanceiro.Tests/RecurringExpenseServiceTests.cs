using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Services;
using Xunit;

namespace ControleFinanceiro.Tests;

public class RecurringExpenseServiceTests
{
    [Fact]
    public async Task GetMonthlyProjection_YearlyChargeOccursOnlyInAnchorMonthAndIsCreatedOnce()
    {
        using var context = TestDbContextFactory.Create();
        context.RecurringExpenses.Add(new RecurringExpense
        {
            Name = "Annual service",
            Amount = 120m,
            PaymentType = "Annual",
            RenewalDay = 29,
            Cadence = RecurringExpense.YearlyCadence,
            InitialRenewalDate = new DateTime(2024, 2, 29)
        });
        await context.SaveChangesAsync();
        var service = new RecurringExpenseService(context);

        var february = await service.GetMonthlyProjectionAsync(2025, 2, CancellationToken.None);
        var march = await service.GetMonthlyProjectionAsync(2025, 3, CancellationToken.None);
        var repeatedFebruary = await service.GetMonthlyProjectionAsync(2025, 2, CancellationToken.None);

        Assert.Equal(120m, february.ScheduledAmount);
        Assert.Equal(new DateTimeOffset(2025, 2, 28, 0, 0, 0, TimeSpan.Zero), february.Occurrences.Single().RenewalDate);
        Assert.Equal(0m, march.ScheduledAmount);
        Assert.Single(repeatedFebruary.Occurrences);
        Assert.Equal(1, context.RecurringExpenseOccurrences.Count());
    }

    [Fact]
    public async Task LinkOccurrence_ExcludesItFromAdditionalCashOutflow()
    {
        using var context = TestDbContextFactory.Create();
        var bill = new ContaCasa
        {
            Descricao = "Annual bill",
            DataVencimento = new DateTime(2026, 4, 30),
            ValorPrevisto = 120m
        };
        context.ContasCasa.Add(bill);
        context.RecurringExpenses.Add(new RecurringExpense
        {
            Name = "Annual service",
            Amount = 120m,
            PaymentType = "Annual",
            RenewalDay = 30,
            Cadence = RecurringExpense.YearlyCadence,
            InitialRenewalDate = new DateTime(2024, 4, 30)
        });
        await context.SaveChangesAsync();
        var service = new RecurringExpenseService(context);
        var projection = await service.GetMonthlyProjectionAsync(2026, 4, CancellationToken.None);

        var linked = await service.LinkOccurrenceAsync(
            projection.Occurrences.Single().Id,
            new() { HouseBillId = bill.Id },
            CancellationToken.None);
        var linkedProjection = await service.GetMonthlyProjectionAsync(2026, 4, CancellationToken.None);

        Assert.Equal(bill.Id, linked!.HouseBillId);
        Assert.Equal(120m, linkedProjection.ScheduledAmount);
        Assert.Equal(0m, linkedProjection.AdditionalCashOutflowAmount);
    }
}
