using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControleFinanceiro.Tests;

public class FinancialAccountsControllerTests
{
    [Fact]
    public async Task CreateBankAccount_MasksIdentifiersAndCalculatesAvailableOverdraft()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new BankAccountsController(new FinancialAccountService(context));

        var result = await controller.CreateBankAccount(new BankAccountRequest
        {
            BankName = "Example Bank",
            AccountName = "Household",
            AccountType = "Checking",
            BranchIdentifier = "00123",
            AccountIdentifier = "99887766",
            Balance = -100m,
            OverdraftUsed = 250m,
            OverdraftLimit = 1000m
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<BankAccountResponse>(created.Value);
        Assert.Equal("***23", response.BranchIdentifier);
        Assert.Equal("****7766", response.AccountIdentifier);
        Assert.Equal(750m, response.AvailableOverdraft);
        Assert.Equal("99887766", (await context.BankAccounts.SingleAsync()).AccountIdentifier);
    }

    [Fact]
    public async Task CreditCardEndpointsPreserveLimitsAndCapabilities()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CreditCardsController(new FinancialAccountService(context));

        var result = await controller.CreateCreditCard(new CreditCardRequest
        {
            Issuer = "Example Issuer",
            DisplayName = "Daily card",
            PurchaseLimit = 2500m,
            LocalCashWithdrawalLimit = 100m,
            InternationalCashWithdrawalLimit = 200m,
            CreditPixEnabled = true,
            BillPaymentEnabled = false,
            DueDay = 12
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<CreditCardResponse>(created.Value);
        Assert.Equal(2500m, response.PurchaseLimit);
        Assert.Equal(100m, response.LocalCashWithdrawalLimit);
        Assert.Equal(200m, response.InternationalCashWithdrawalLimit);
        Assert.True(response.CreditPixEnabled);
        Assert.False(response.BillPaymentEnabled);
        Assert.Equal(12, response.DueDay);
    }
}