using ControleFinanceiro.API.Models;
using Xunit;

namespace ControleFinanceiro.Tests;

public class RecurringExpenseTests
{
    [Theory]
    [InlineData(2026, 1, 31, 2026, 2, 28)]
    [InlineData(2026, 1, 31, 2026, 4, 30)]
    [InlineData(2026, 4, 30, 2026, 5, 30)]
    public void GetRenewalDate_MonthlyCadenceUsesAnchorDayAndClampsToMonthEnd(
        int anchorYear,
        int anchorMonth,
        int anchorDay,
        int year,
        int month,
        int expectedDay)
    {
        var expense = new RecurringExpense
        {
            Cadence = RecurringExpense.MonthlyCadence,
            InitialRenewalDate = new DateTime(anchorYear, anchorMonth, anchorDay)
        };

        Assert.Equal(new DateTime(year, month, expectedDay), expense.GetRenewalDate(year, month));
    }

    [Fact]
    public void GetRenewalDate_MonthlyCadenceDoesNotProjectBeforeInitialRenewalDate()
    {
        var expense = new RecurringExpense
        {
            Cadence = RecurringExpense.MonthlyCadence,
            InitialRenewalDate = new DateTime(2026, 4, 15)
        };

        Assert.Null(expense.GetRenewalDate(2026, 3));
    }

    [Theory]
    [InlineData(2024, 2, 29, 2025, 2, 28)]
    [InlineData(2024, 2, 29, 2028, 2, 29)]
    public void GetRenewalDate_YearlyCadenceUsesAnchorMonthAndClampsLeapDay(
        int anchorYear,
        int anchorMonth,
        int anchorDay,
        int year,
        int month,
        int expectedDay)
    {
        var expense = new RecurringExpense
        {
            Cadence = RecurringExpense.YearlyCadence,
            InitialRenewalDate = new DateTime(anchorYear, anchorMonth, anchorDay)
        };

        Assert.Equal(new DateTime(year, month, expectedDay), expense.GetRenewalDate(year, month));
    }

    [Theory]
    [InlineData(2024, 2, 29, 2025, 3)]
    [InlineData(2024, 2, 29, 2023, 2)]
    public void GetRenewalDate_YearlyCadenceOnlyProjectsAnchorMonthOnOrAfterInitialDate(
        int anchorYear,
        int anchorMonth,
        int anchorDay,
        int year,
        int month)
    {
        var expense = new RecurringExpense
        {
            Cadence = RecurringExpense.YearlyCadence,
            InitialRenewalDate = new DateTime(anchorYear, anchorMonth, anchorDay)
        };

        Assert.Null(expense.GetRenewalDate(year, month));
    }

}