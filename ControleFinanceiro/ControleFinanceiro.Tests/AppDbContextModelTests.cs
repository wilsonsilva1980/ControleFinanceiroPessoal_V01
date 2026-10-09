using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControleFinanceiro.Tests;

public class AppDbContextModelTests
{
    [Fact]
    public void SqlServerModel_ContainsIdentityTablesAndMappings()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=controle_financeiro;Trusted_Connection=True")
            .Options;

        using var context = new AppDbContext(options);
        var model = context.Model;
        var identityTables = model.GetEntityTypes()
            .Select(entity => entity.GetTableName()!)
            .Where(tableName => tableName.StartsWith("AspNet", StringComparison.Ordinal))
            .ToArray();

        var expectedTables = new[]
        {
            "AspNetRoleClaims",
            "AspNetRoles",
            "AspNetUserClaims",
            "AspNetUserLogins",
            "AspNetUserRoles",
            "AspNetUserTokens",
            "AspNetUsers"
        };

        Assert.Equal(expectedTables.Length, identityTables.Length);
        Assert.All(expectedTables, tableName => Assert.Contains(tableName, identityTables));

        var users = model.FindEntityType(typeof(IdentityUser))!;
        Assert.Equal("AspNetUsers", users.GetTableName());
        Assert.Equal("nvarchar(450)", users.FindProperty(nameof(IdentityUser.Id))!.GetColumnType());
        Assert.Equal("nvarchar(256)", users.FindProperty(nameof(IdentityUser.NormalizedUserName))!.GetColumnType());
        Assert.Equal("datetimeoffset", users.FindProperty(nameof(IdentityUser.LockoutEnd))!.GetColumnType());

        var usernameIndex = users.GetIndexes().Single(index =>
            index.Properties.Any(property => property.Name == nameof(IdentityUser.NormalizedUserName)));
        Assert.True(usernameIndex.IsUnique);
        Assert.Contains("IS NOT NULL", usernameIndex.GetFilter(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SqlServerModel_MapsFinancialAccountsAndCardsToSchemaNames()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=controle_financeiro;Trusted_Connection=True")
            .Options;

        using var context = new AppDbContext(options);
        var account = context.Model.FindEntityType(typeof(BankAccount))!;
        var card = context.Model.FindEntityType(typeof(CreditCard))!;

        Assert.Equal("bank_accounts", account.GetTableName());
        Assert.Equal("branch_identifier", account.FindProperty(nameof(BankAccount.BranchIdentifier))!.GetColumnName());
        Assert.Equal("nvarchar(30)", account.FindProperty(nameof(BankAccount.BranchIdentifier))!.GetColumnType());
        Assert.Equal("decimal(10,2)", account.FindProperty(nameof(BankAccount.OverdraftLimit))!.GetColumnType());
        Assert.Null(account.FindProperty(nameof(BankAccount.AvailableOverdraft)));
        Assert.Equal("credit_cards", card.GetTableName());
        Assert.Equal("international_cash_withdrawal_limit", card.FindProperty(nameof(CreditCard.InternationalCashWithdrawalLimit))!.GetColumnName());
        Assert.Equal("due_day", card.FindProperty(nameof(CreditCard.DueDay))!.GetColumnName());
    }

    [Fact]
    public void SqlServerModel_MapsRecurringDefinitionsAndOccurrences()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=controle_financeiro;Trusted_Connection=True")
            .Options;

        using var context = new AppDbContext(options);
        var definition = context.Model.FindEntityType(typeof(RecurringExpense))!;
        var occurrence = context.Model.FindEntityType(typeof(RecurringExpenseOccurrence))!;

        Assert.Equal("recurring_expenses", definition.GetTableName());
        Assert.Equal("cadence", definition.FindProperty(nameof(RecurringExpense.Cadence))!.GetColumnName());
        Assert.Equal("initial_renewal_date", definition.FindProperty(nameof(RecurringExpense.InitialRenewalDate))!.GetColumnName());
        Assert.Equal("date", definition.FindProperty(nameof(RecurringExpense.InitialRenewalDate))!.GetColumnType());
        Assert.Equal("recurring_expense_occurrences", occurrence.GetTableName());
        Assert.Equal("date", occurrence.FindProperty(nameof(RecurringExpenseOccurrence.RenewalDate))!.GetColumnType());
        Assert.Equal("conta_casa_id", occurrence.FindProperty(nameof(RecurringExpenseOccurrence.ContaCasaId))!.GetColumnName());
        Assert.Equal("despesa_variavel_id", occurrence.FindProperty(nameof(RecurringExpenseOccurrence.DespesaVariavelId))!.GetColumnName());
    }

    [Fact]
    public void SqlServerModel_MapsTripBudgetItemsInstallmentsAndPayments()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=controle_financeiro;Trusted_Connection=True")
            .Options;

        using var context = new AppDbContext(options);
        var budget = context.Model.FindEntityType(typeof(TripBudget))!;
        var item = context.Model.FindEntityType(typeof(TripCostItem))!;
        var installment = context.Model.FindEntityType(typeof(TripInstallment))!;
        var payment = context.Model.FindEntityType(typeof(TripPayment))!;

        Assert.Equal("trip_budgets", budget.GetTableName());
        Assert.Equal("clp_per_brl_quote", budget.FindProperty(nameof(TripBudget.ClpPerBrlQuote))!.GetColumnName());
        Assert.Equal("trip_cost_items", item.GetTableName());
        Assert.Equal("value_brl", item.FindProperty(nameof(TripCostItem.ValueBRL))!.GetColumnName());
        Assert.Equal("trip_installments", installment.GetTableName());
        Assert.Equal("date", installment.FindProperty(nameof(TripInstallment.DueDate))!.GetColumnType());
        Assert.Equal("trip_payments", payment.GetTableName());
        Assert.Equal("trip_installment_id", payment.FindProperty(nameof(TripPayment.TripInstallmentId))!.GetColumnName());
        Assert.Null(item.FindProperty(nameof(TripCostItem.TotalBRL)));
    }
}