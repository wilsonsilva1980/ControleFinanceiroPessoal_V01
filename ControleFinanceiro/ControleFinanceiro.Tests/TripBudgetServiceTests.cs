using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Services;
using Xunit;

namespace ControleFinanceiro.Tests;

public class TripBudgetServiceTests
{
    [Fact]
    public async Task TripBudgetService_CreatesItemsAndKeepsInstallmentPaymentsInDerivedBalance()
    {
        using var context = TestDbContextFactory.Create();
        var service = new TripBudgetService(context);
        var budget = await service.CreateAsync(
            new CreateTripBudgetRequest
            {
                Name = "Chile",
                Destination = "Santiago",
                ClpPerBrlQuote = 200m
            },
            CancellationToken.None);
        var item = await service.CreateItemAsync(
            budget.Id,
            new CreateTripCostItemRequest
            {
                Description = "Hotel",
                ValueBRL = 1000m,
                FeesBRL = 100m,
                ValueUSD = 210m,
                InstallmentCount = 2,
                Installments =
                [
                    new() { DueDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero), Amount = 550m },
                    new() { DueDate = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero), Amount = 550m }
                ]
            },
            CancellationToken.None);
        var createdItem = Assert.IsType<TripCostItemResponse>(item);
        await service.CreatePaymentAsync(
            createdItem.Id,
            new CreateTripPaymentRequest
            {
                PaymentDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero),
                Amount = 300m,
                TripInstallmentId = createdItem.Installments[0].Id
            },
            CancellationToken.None);

        var result = await service.GetByIdAsync(budget.Id, CancellationToken.None);

        var verifiedResult = Assert.IsType<TripBudgetResponse>(result);
        Assert.Equal(220000m, verifiedResult.ClpTotal);
        Assert.Equal(1100m, verifiedResult.TotalBRL);
        Assert.Equal(300m, verifiedResult.PaidAmount);
        Assert.Equal(800m, verifiedResult.RemainingAmount);
        Assert.Equal(2, Assert.Single(verifiedResult.Items).Installments.Count);
        Assert.Equal(300m, verifiedResult.Items[0].Payments.Single().Amount);
    }
}
