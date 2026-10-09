using ControleFinanceiro.API.Models;
using Xunit;

namespace ControleFinanceiro.Tests;

public class DebtInstallmentTests
{
    [Theory]
    [InlineData(DebtInstallment.PendingStatus, 0, 125.50)]
    [InlineData(DebtInstallment.PaidStatus, 125.50, 0)]
    public void PaidAndRemainingAmounts_FollowInstallmentStatus(
        string status,
        decimal expectedPaid,
        decimal expectedRemaining)
    {
        var installment = new DebtInstallment
        {
            ScheduledAmount = 125.50m,
            Status = status
        };

        Assert.Equal(expectedPaid, installment.PaidAmount);
        Assert.Equal(expectedRemaining, installment.RemainingAmount);
    }
}