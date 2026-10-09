using ControleFinanceiro.API.Models;
using Xunit;

namespace ControleFinanceiro.Tests;

public class TripBudgetTests
{
    [Fact]
    public void TripCostItem_CalculatesTotalFromValueAndFeesAndSubtractsPayments()
    {
        var item = new TripCostItem
        {
            Description = "Lodging",
            ValueBRL = 1000m,
            FeesBRL = 125.50m,
            Payments =
            [
                new TripPayment { Amount = 300m, PaymentDate = new DateTime(2026, 10, 1) },
                new TripPayment { Amount = 200m, PaymentDate = new DateTime(2026, 10, 2) }
            ]
        };

        Assert.Equal(1125.50m, item.TotalBRL);
        Assert.Equal(500m, item.PaidAmount);
        Assert.Equal(625.50m, item.RemainingAmount);
    }

    [Fact]
    public void TripBudget_DerivesPeriodTotalsFromItsItems()
    {
        var budget = new TripBudget
        {
            Name = "Chile",
            Items =
            [
                new TripCostItem
                {
                    Description = "Flight",
                    ValueBRL = 1500m,
                    FeesBRL = 0m,
                    Payments = [new TripPayment { Amount = 500m }]
                },
                new TripCostItem
                {
                    Description = "Hotel",
                    ValueBRL = 800m,
                    FeesBRL = 100m
                }
            ]
        };

        Assert.Equal(2400m, budget.TotalBRL);
        Assert.Equal(500m, budget.PaidAmount);
        Assert.Equal(1900m, budget.RemainingAmount);
    }

    [Fact]
    public void TripBudget_ConvertsBrlTotalUsingStoredClpPerBrlQuote()
    {
        var budget = new TripBudget
        {
            Name = "Chile",
            ClpPerBrlQuote = 200.5m,
            Items =
            [
                new TripCostItem { Description = "Flight", ValueBRL = 1000m, FeesBRL = 100m }
            ]
        };

        Assert.Equal(200.5m, budget.ClpPerBrlQuote);
        Assert.Equal(220550m, budget.ClpTotal);
    }
}
