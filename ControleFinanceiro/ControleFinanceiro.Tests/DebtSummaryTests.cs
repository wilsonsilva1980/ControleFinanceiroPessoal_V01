using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ControleFinanceiro.Tests;

public class DebtSummaryTests
{
    [Fact]
    public async Task PeriodSummaries_CountLinkedAndUnlinkedOutflowsOnce()
    {
        using var context = TestDbContextFactory.Create();
        var householdBill = new ContaCasa
        {
            Descricao = "Household bill",
            DataVencimento = new DateTime(2026, 10, 10),
            ValorPrevisto = 100m
        };
        var earlyPaymentBill = new ContaCasa
        {
            Descricao = "Early payment bill",
            DataVencimento = new DateTime(2026, 10, 12),
            ValorPrevisto = 40m
        };
        var cardPurchase = new CartaoLancamento
        {
            Estabelecimento = "Store",
            MesFatura = "outubro",
            AnoFatura = 2026,
            ValorParcela = 200m
        };
        context.AddRange(householdBill, earlyPaymentBill, cardPurchase);
        await context.SaveChangesAsync();

        var debt = new Debt
        {
            Description = "Debt",
            DueDate = new DateTime(2026, 10, 1),
            OriginalAmount = 420m
        };
        var billInstallment = new DebtInstallment
        {
            DueDate = new DateTime(2026, 10, 10),
            ScheduledAmount = 100m
        };
        billInstallment.OutflowLink = new DebtOutflowLink
        {
            DebtInstallment = billInstallment,
            ContaCasaId = householdBill.Id
        };
        var cardInstallment = new DebtInstallment
        {
            DueDate = new DateTime(2026, 10, 15),
            ScheduledAmount = 200m,
            Status = DebtInstallment.PaidStatus
        };
        cardInstallment.OutflowLink = new DebtOutflowLink
        {
            DebtInstallment = cardInstallment,
            CartaoLancamentoId = cardPurchase.Id
        };
        debt.Installments.Add(billInstallment);
        debt.Installments.Add(cardInstallment);
        debt.Installments.Add(new DebtInstallment
        {
            DueDate = new DateTime(2026, 10, 20),
            ScheduledAmount = 50m
        });
        debt.Installments.Add(new DebtInstallment
        {
            DueDate = new DateTime(2026, 11, 20),
            ScheduledAmount = 70m
        });
        var linkedEarlyPayment = new DebtEarlyPayment
        {
            PaymentDate = new DateTime(2026, 10, 12),
            Amount = 40m
        };
        linkedEarlyPayment.OutflowLink = new DebtOutflowLink
        {
            DebtEarlyPayment = linkedEarlyPayment,
            ContaCasaId = earlyPaymentBill.Id
        };
        debt.EarlyPayments.Add(linkedEarlyPayment);
        debt.EarlyPayments.Add(new DebtEarlyPayment
        {
            PaymentDate = new DateTime(2026, 10, 25),
            Amount = 30m
        });
        context.Debts.Add(debt);
        context.DespesasVariaveis.Add(new DespesaVariavel
        {
            Data = new DateTime(2026, 10, 18),
            Descricao = "Variable expense",
            Valor = 25m
        });
        await context.SaveChangesAsync();

        var controller = new DebtsController(
            new DebtService(context, new RecurringExpenseService(context)),
            new DebtWorkbookImportService(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=ControleFinanceiro;Trusted_Connection=True;"
                })
                .Build(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<DebtWorkbookImportService>.Instance));
        var monthlyResult = await controller.GetMonthlySummary(2026, 10, CancellationToken.None);
        var yearlyResult = await controller.GetYearlySummary(2026, CancellationToken.None);

        var monthly = Assert.IsType<DebtPeriodSummaryResponse>(Assert.IsType<OkObjectResult>(monthlyResult.Result).Value);
        var yearly = Assert.IsType<DebtPeriodSummaryResponse>(Assert.IsType<OkObjectResult>(yearlyResult.Result).Value);
        Assert.Equal(350m, monthly.ScheduledAmount);
        Assert.Equal(270m, monthly.PaidAmount);
        Assert.Equal(80m, monthly.RemainingAmount);
        Assert.Equal(445m, monthly.CashOutflowAmount);
        Assert.Equal(420m, yearly.ScheduledAmount);
        Assert.Equal(515m, yearly.CashOutflowAmount);
    }
}