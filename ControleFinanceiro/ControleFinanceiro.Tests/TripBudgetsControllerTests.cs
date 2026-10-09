using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ControleFinanceiro.Tests;

public class TripBudgetsControllerTests
{
    [Fact]
    public async Task TripBudgetController_SupportsCreateReadUpdateAndDelete()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new TripBudgetsController(new TripBudgetService(context));
        var request = new CreateTripBudgetRequest
        {
            Name = "Chile",
            Destination = "Santiago",
            ClpPerBrlQuote = 200m
        };

        var create = await controller.CreateTripBudget(request, CancellationToken.None);
        var created = Assert.IsType<TripBudgetResponse>(Assert.IsType<CreatedAtActionResult>(create.Result).Value);
        var read = await controller.GetTripBudget(created.Id, CancellationToken.None);
        var fetched = Assert.IsType<TripBudgetResponse>(Assert.IsType<OkObjectResult>(read.Result).Value);
        var update = await controller.UpdateTripBudget(
            created.Id,
            new UpdateTripBudgetRequest
            {
                Name = "Chile 2027",
                Destination = "Valparaíso",
                ClpPerBrlQuote = 210m
            },
            CancellationToken.None);
        var updated = await controller.GetTripBudget(created.Id, CancellationToken.None);
        var delete = await controller.DeleteTripBudget(created.Id, CancellationToken.None);
        var missing = await controller.GetTripBudget(created.Id, CancellationToken.None);

        Assert.Equal(200m, fetched.ClpPerBrlQuote);
        Assert.IsType<NoContentResult>(update);
        Assert.Equal("Chile 2027", Assert.IsType<TripBudgetResponse>(Assert.IsType<OkObjectResult>(updated.Result).Value).Name);
        Assert.IsType<NoContentResult>(delete);
        Assert.IsType<NotFoundResult>(missing.Result);
    }

    [Fact]
    public async Task CreateTripCostItem_RejectsInstallmentsThatDoNotMatchTotal()
    {
        using var context = TestDbContextFactory.Create();
        var budget = new TripBudget { Name = "Chile", Destination = "Santiago" };
        context.TripBudgets.Add(budget);
        await context.SaveChangesAsync();
        var controller = new TripBudgetsController(new TripBudgetService(context));

        var result = await controller.CreateTripCostItem(
            budget.Id,
            new CreateTripCostItemRequest
            {
                Description = "Hotel",
                ValueBRL = 1000m,
                FeesBRL = 100m,
                InstallmentCount = 2,
                Installments =
                [
                    new() { DueDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero), Amount = 500m },
                    new() { DueDate = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero), Amount = 500m }
                ]
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.TripCostItems);
    }
}
