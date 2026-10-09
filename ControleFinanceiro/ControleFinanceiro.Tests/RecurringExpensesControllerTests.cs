using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ControleFinanceiro.Tests;

public class RecurringExpensesControllerTests
{
    [Fact]
    public async Task RecurringExpenseEndpoints_CreateReadUpdateAndDeleteCharge()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new RecurringExpensesController(new RecurringExpenseService(context));
        var request = new RecurringExpenseRequest
        {
            Name = "Internet",
            ChargeType = RecurringExpense.HouseholdType,
            Amount = 99.90m,
            PaymentType = "Credit card",
            Cadence = RecurringExpense.MonthlyCadence,
            InitialRenewalDate = new DateTime(2026, 1, 31)
        };

        var createdResult = await controller.CreateRecurringExpense(request, CancellationToken.None);
        var created = Assert.IsType<RecurringExpenseResponse>(
            Assert.IsType<CreatedAtActionResult>(createdResult.Result).Value);
        var fetchedResult = await controller.GetRecurringExpense(created.Id, CancellationToken.None);
        var fetched = Assert.IsType<RecurringExpenseResponse>(Assert.IsType<OkObjectResult>(fetchedResult.Result).Value);

        var updatedResult = await controller.UpdateRecurringExpense(
            created.Id,
            request with { Cadence = RecurringExpense.YearlyCadence },
            CancellationToken.None);
        var updated = await context.RecurringExpenses.FindAsync(created.Id);
        var deletedResult = await controller.DeleteRecurringExpense(created.Id, CancellationToken.None);
        var missingResult = await controller.GetRecurringExpense(created.Id, CancellationToken.None);

        Assert.Equal(RecurringExpense.MonthlyCadence, fetched.Cadence);
        Assert.Equal(new DateTime(2026, 1, 31), fetched.InitialRenewalDate);
        Assert.IsType<NoContentResult>(updatedResult);
        Assert.Equal(RecurringExpense.YearlyCadence, updated!.Cadence);
        Assert.IsType<NoContentResult>(deletedResult);
        Assert.IsType<NotFoundResult>(missingResult.Result);
    }

    [Fact]
    public async Task MonthlyProjectionEndpoint_ProjectsOnlyDueYearlyCharges()
    {
        using var context = TestDbContextFactory.Create();
        context.RecurringExpenses.Add(new RecurringExpense
        {
            Name = "Annual subscription",
            ChargeType = RecurringExpense.ServiceType,
            Amount = 49.99m,
            PaymentType = "Card",
            Cadence = RecurringExpense.YearlyCadence,
            InitialRenewalDate = new DateTime(2026, 10, 15)
        });
        await context.SaveChangesAsync();
        var controller = new RecurringExpensesController(new RecurringExpenseService(context));

        var dueResult = await controller.GetMonthlyProjection(2026, 10, CancellationToken.None);
        var otherMonthResult = await controller.GetMonthlyProjection(2026, 11, CancellationToken.None);
        var invalidMonthResult = await controller.GetMonthlyProjection(2026, 13, CancellationToken.None);

        var due = Assert.IsType<RecurringExpenseProjectionResponse>(Assert.IsType<OkObjectResult>(dueResult.Result).Value);
        var otherMonth = Assert.IsType<RecurringExpenseProjectionResponse>(Assert.IsType<OkObjectResult>(otherMonthResult.Result).Value);
        Assert.Single(due.Occurrences);
        Assert.Equal(49.99m, due.ScheduledAmount);
        Assert.Empty(otherMonth.Occurrences);
        Assert.IsType<BadRequestObjectResult>(invalidMonthResult.Result);
    }

    [Fact]
    public async Task CreateRecurringExpense_RejectsUnsupportedCadence()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new RecurringExpensesController(new RecurringExpenseService(context));
        var request = new RecurringExpenseRequest
        {
            Name = "Subscription",
            ChargeType = RecurringExpense.ServiceType,
            Amount = 25m,
            PaymentType = "Card",
            Cadence = "Weekly",
            InitialRenewalDate = new DateTime(2026, 10, 15)
        };

        var result = await controller.CreateRecurringExpense(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.RecurringExpenses);
    }
}
