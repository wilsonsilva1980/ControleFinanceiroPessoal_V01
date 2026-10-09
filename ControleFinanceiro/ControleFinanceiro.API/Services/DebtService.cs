using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ControleFinanceiro.API.Services;

public sealed class DebtService(AppDbContext context, IRecurringExpenseService recurringExpenses) : IDebtService
{
    public async Task<IReadOnlyList<DebtResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var debts = await context.Debts
            .AsNoTracking()
            .Include(debt => debt.Installments)
                .ThenInclude(installment => installment.OutflowLink)
            .Include(debt => debt.EarlyPayments)
                .ThenInclude(payment => payment.OutflowLink)
            .OrderBy(debt => debt.DueDate)
            .ToListAsync(cancellationToken);

        return debts.Select(ToResponse).ToArray();
    }

    public async Task<DebtResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var debt = await context.Debts
            .AsNoTracking()
            .Include(item => item.Installments)
                .ThenInclude(installment => installment.OutflowLink)
            .Include(item => item.EarlyPayments)
                .ThenInclude(payment => payment.OutflowLink)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return debt is null ? null : ToResponse(debt);
    }

    public async Task<DebtResponse> CreateAsync(CreateDebtRequest request, CancellationToken cancellationToken)
    {
        var usedHouseBillIds = new HashSet<int>();
        var usedCardPurchaseIds = new HashSet<int>();
        var installments = new List<DebtInstallment>();
        foreach (var installmentRequest in request.Installments)
        {
            var installment = new DebtInstallment
            {
                DueDate = ToSqlDate(installmentRequest.DueDate),
                ScheduledAmount = installmentRequest.ScheduledAmount
            };
            installment.OutflowLink = await CreateOutflowLinkAsync(
                installment,
                null,
                installmentRequest.LinkedHouseBillId,
                installmentRequest.LinkedCardPurchaseId,
                usedHouseBillIds,
                usedCardPurchaseIds,
                cancellationToken);
            installments.Add(installment);
        }

        var debt = new Debt
        {
            Description = request.Description.Trim(),
            DueDate = ToSqlDate(request.DueDate),
            OriginalAmount = request.OriginalAmount,
            CreditLimit = request.CreditLimit,
            Installments = installments
        };

        context.Debts.Add(debt);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(debt);
    }

    public async Task<DebtInstallmentResponse?> UpdateInstallmentPaymentAsync(
        int installmentId,
        UpdateDebtInstallmentPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var installment = await context.DebtInstallments
            .Include(item => item.OutflowLink)
            .SingleOrDefaultAsync(item => item.Id == installmentId, cancellationToken);
        if (installment is null)
        {
            return null;
        }

        installment.Status = request.Status;
        installment.PaymentDate = request.Status == DebtInstallment.PaidStatus && request.PaymentDate.HasValue
            ? ToSqlDate(request.PaymentDate.Value)
            : null;
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(installment);
    }

    public async Task<DebtEarlyPaymentResponse?> CreateEarlyPaymentAsync(
        int debtId,
        CreateDebtEarlyPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await context.Debts.AnyAsync(debt => debt.Id == debtId, cancellationToken))
        {
            return null;
        }

        var payment = new DebtEarlyPayment
        {
            DebtId = debtId,
            PaymentDate = ToSqlDate(request.PaymentDate),
            Amount = request.Amount
        };
        payment.OutflowLink = await CreateOutflowLinkAsync(
            null,
            payment,
            request.LinkedHouseBillId,
            request.LinkedCardPurchaseId,
            [],
            [],
            cancellationToken);
        context.DebtEarlyPayments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(payment);
    }

    public async Task<DebtPeriodSummaryResponse> GetPeriodSummaryAsync(
        int year,
        int? month,
        CancellationToken cancellationToken)
    {
        var start = month is { } monthNumber
            ? new DateTime(year, monthNumber, 1)
            : new DateTime(year, 1, 1);
        var end = month is { } periodMonth
            ? start.AddMonths(1)
            : start.AddYears(1);

        if (month is { } selectedMonth)
        {
            await recurringExpenses.EnsureMonthlyProjectionAsync(year, selectedMonth, cancellationToken);
        }
        else
        {
            for (var projectionMonth = 1; projectionMonth <= 12; projectionMonth++)
            {
                await recurringExpenses.EnsureMonthlyProjectionAsync(year, projectionMonth, cancellationToken);
            }
        }

        var installments = await context.DebtInstallments
            .AsNoTracking()
            .Where(item => item.DueDate >= start && item.DueDate < end)
            .Select(item => new
            {
                item.ScheduledAmount,
                IsPaid = item.Status == DebtInstallment.PaidStatus,
                IsLinked = context.DebtOutflowLinks.Any(link => link.DebtInstallmentId == item.Id)
            })
            .ToListAsync(cancellationToken);

        var earlyPayments = await context.DebtEarlyPayments
            .AsNoTracking()
            .Where(item => item.PaymentDate >= start && item.PaymentDate < end)
            .Select(item => new
            {
                item.Amount,
                IsLinked = context.DebtOutflowLinks.Any(link => link.DebtEarlyPaymentId == item.Id)
            })
            .ToListAsync(cancellationToken);

        var householdExpenses = await context.ContasCasa
            .Where(item => item.DataVencimento >= start && item.DataVencimento < end)
            .SumAsync(item => (decimal?)(item.ValorPago ?? item.ValorPrevisto), cancellationToken) ?? 0m;

        var variableExpenses = await context.DespesasVariaveis
            .Where(item => item.Data >= start && item.Data < end)
            .SumAsync(item => (decimal?)item.Valor, cancellationToken) ?? 0m;

        var cardExpensesQuery = context.CartaoLancamentos
            .Where(item => item.AnoFatura == year);
        if (month is { } invoiceMonth)
        {
            var monthName = new DateTime(year, invoiceMonth, 1)
                .ToString("MMMM", CultureInfo.GetCultureInfo("pt-BR"));
            cardExpensesQuery = cardExpensesQuery.Where(item => item.MesFatura.ToLower() == monthName);
        }

        var cardExpenses = await cardExpensesQuery
            .SumAsync(item => (decimal?)item.ValorParcela, cancellationToken) ?? 0m;

        var recurringOutflow = await context.RecurringExpenseOccurrences
            .Where(item => item.RenewalDate >= start && item.RenewalDate < end)
            .Where(item => item.ContaCasaId == null && item.DespesaVariavelId == null)
            .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;

        var scheduledAmount = installments.Sum(item => item.ScheduledAmount);
        var paidAmount = installments
            .Where(item => item.IsPaid)
            .Sum(item => item.ScheduledAmount)
            + earlyPayments.Sum(item => item.Amount);
        var cashOutflowAmount = householdExpenses
            + variableExpenses
            + cardExpenses
            + recurringOutflow
            + installments.Where(item => !item.IsLinked).Sum(item => item.ScheduledAmount)
            + earlyPayments.Where(item => !item.IsLinked).Sum(item => item.Amount);

        return new DebtPeriodSummaryResponse(
            year,
            month,
            scheduledAmount,
            paidAmount,
            Math.Max(0m, scheduledAmount - paidAmount),
            cashOutflowAmount);
    }

    private async Task<DebtOutflowLink?> CreateOutflowLinkAsync(
        DebtInstallment? installment,
        DebtEarlyPayment? payment,
        int? houseBillId,
        int? cardPurchaseId,
        HashSet<int> linkedHouseBillIds,
        HashSet<int> linkedCardPurchaseIds,
        CancellationToken cancellationToken)
    {
        if (houseBillId is null && cardPurchaseId is null)
        {
            return null;
        }

        if (houseBillId.HasValue == cardPurchaseId.HasValue)
        {
            throw new DebtOutflowLinkException(
                DebtOutflowLinkFailure.InvalidCombination,
                "Choose exactly one household bill or card purchase to link.");
        }

        if (houseBillId is { } billId)
        {
            if (!await context.ContasCasa.AnyAsync(item => item.Id == billId, cancellationToken))
            {
                throw new DebtOutflowLinkException(
                    DebtOutflowLinkFailure.LedgerRecordNotFound,
                    "The linked household bill was not found.");
            }

            if (!linkedHouseBillIds.Add(billId)
                || await context.DebtOutflowLinks.AnyAsync(link => link.ContaCasaId == billId, cancellationToken))
            {
                throw new DebtOutflowLinkException(
                    DebtOutflowLinkFailure.AlreadyLinked,
                    "The household bill is already linked to another debt outflow.");
            }
        }

        if (cardPurchaseId is { } purchaseId)
        {
            if (!await context.CartaoLancamentos.AnyAsync(item => item.Id == purchaseId, cancellationToken))
            {
                throw new DebtOutflowLinkException(
                    DebtOutflowLinkFailure.LedgerRecordNotFound,
                    "The linked card purchase was not found.");
            }

            if (!linkedCardPurchaseIds.Add(purchaseId)
                || await context.DebtOutflowLinks.AnyAsync(link => link.CartaoLancamentoId == purchaseId, cancellationToken))
            {
                throw new DebtOutflowLinkException(
                    DebtOutflowLinkFailure.AlreadyLinked,
                    "The card purchase is already linked to another debt outflow.");
            }
        }

        return new DebtOutflowLink
        {
            DebtInstallment = installment,
            DebtEarlyPayment = payment,
            ContaCasaId = houseBillId,
            CartaoLancamentoId = cardPurchaseId
        };
    }

    private static DebtResponse ToResponse(Debt debt) => new(
        debt.Id,
        debt.Description,
        ToDateOffset(debt.DueDate),
        debt.OriginalAmount,
        debt.CreditLimit,
        debt.Installments
            .OrderBy(installment => installment.DueDate)
            .Select(ToResponse)
            .ToArray(),
        debt.EarlyPayments
            .OrderBy(payment => payment.PaymentDate)
            .Select(ToResponse)
            .ToArray());

    private static DebtInstallmentResponse ToResponse(DebtInstallment installment) => new(
        installment.Id,
        ToDateOffset(installment.DueDate),
        installment.ScheduledAmount,
        installment.Status,
        installment.PaymentDate is { } paymentDate ? ToDateOffset(paymentDate) : null,
        installment.PaidAmount,
        installment.RemainingAmount,
        installment.OutflowLink?.ContaCasaId,
        installment.OutflowLink?.CartaoLancamentoId);

    private static DebtEarlyPaymentResponse ToResponse(DebtEarlyPayment payment) => new(
        payment.Id,
        ToDateOffset(payment.PaymentDate),
        payment.Amount,
        payment.OutflowLink?.ContaCasaId,
        payment.OutflowLink?.CartaoLancamentoId);

    private static DateTime ToSqlDate(DateTimeOffset value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified);

    private static DateTimeOffset ToDateOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified), TimeSpan.Zero);
}