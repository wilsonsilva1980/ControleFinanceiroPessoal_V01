using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Services;

public sealed class RecurringExpenseService(AppDbContext context) : IRecurringExpenseService
{
    public async Task<IReadOnlyList<RecurringExpenseResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        (await context.RecurringExpenses.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken))
        .Select(ToResponse)
        .ToArray();

    public async Task<RecurringExpenseResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var expense = await context.RecurringExpenses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return expense is null ? null : ToResponse(expense);
    }

    public async Task<RecurringExpenseResponse> CreateAsync(
        RecurringExpenseRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var expense = new RecurringExpense();
        Apply(expense, request);
        context.RecurringExpenses.Add(expense);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(expense);
    }

    public async Task<bool> UpdateAsync(
        int id,
        RecurringExpenseRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var expense = await context.RecurringExpenses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (expense is null) return false;
        Apply(expense, request);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var expense = await context.RecurringExpenses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (expense is null) return false;
        context.RecurringExpenses.Remove(expense);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task EnsureMonthlyProjectionAsync(int year, int month, CancellationToken cancellationToken)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var definitions = await context.RecurringExpenses.AsNoTracking().ToListAsync(cancellationToken);
        var existingIds = await context.RecurringExpenseOccurrences
            .Where(item => item.RenewalDate >= start && item.RenewalDate < end)
            .Select(item => item.RecurringExpenseId)
            .ToListAsync(cancellationToken);
        var existingSet = existingIds.ToHashSet();
        var createdOccurrence = false;

        foreach (var definition in definitions)
        {
            if (existingSet.Contains(definition.Id)) continue;
            var renewalDate = definition.GetRenewalDate(year, month);
            if (renewalDate is null) continue;
            context.RecurringExpenseOccurrences.Add(new RecurringExpenseOccurrence
            {
                RecurringExpenseId = definition.Id,
                RenewalDate = renewalDate.Value,
                Amount = definition.Amount
            });
            createdOccurrence = true;
        }

        if (createdOccurrence)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<RecurringExpenseProjectionResponse> GetMonthlyProjectionAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        await EnsureMonthlyProjectionAsync(year, month, cancellationToken);
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var occurrences = await context.RecurringExpenseOccurrences
            .AsNoTracking()
            .Include(item => item.RecurringExpense)
            .Where(item => item.RenewalDate >= start && item.RenewalDate < end)
            .OrderBy(item => item.RenewalDate)
            .ThenBy(item => item.RecurringExpense.Name)
            .ToListAsync(cancellationToken);
        var responses = occurrences.Select(ToResponse).ToArray();

        return new RecurringExpenseProjectionResponse(
            year,
            month,
            responses,
            responses.Sum(item => item.Amount),
            occurrences
                .Where(item => item.ContaCasaId is null && item.DespesaVariavelId is null)
                .Sum(item => item.Amount));
    }

    public async Task<RecurringExpenseOccurrenceResponse?> LinkOccurrenceAsync(
        int occurrenceId,
        LinkRecurringExpenseOccurrenceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.HouseBillId.HasValue == request.VariableExpenseId.HasValue)
        {
            throw new RecurringExpenseLinkException(
                RecurringExpenseLinkFailure.InvalidCombination,
                "Choose exactly one household bill or variable expense to link.");
        }

        var occurrence = await context.RecurringExpenseOccurrences
            .Include(item => item.RecurringExpense)
            .SingleOrDefaultAsync(item => item.Id == occurrenceId, cancellationToken);
        if (occurrence is null) return null;
        if (occurrence.ContaCasaId is not null || occurrence.DespesaVariavelId is not null)
        {
            throw new RecurringExpenseLinkException(
                RecurringExpenseLinkFailure.AlreadyLinked,
                "This recurring occurrence is already linked to an expense.");
        }

        if (request.HouseBillId is { } billId)
        {
            if (!await context.ContasCasa.AnyAsync(item => item.Id == billId, cancellationToken))
            {
                throw new RecurringExpenseLinkException(
                    RecurringExpenseLinkFailure.LedgerRecordNotFound,
                    "The linked household bill was not found.");
            }

            if (await context.RecurringExpenseOccurrences.AnyAsync(item => item.ContaCasaId == billId, cancellationToken))
            {
                throw new RecurringExpenseLinkException(
                    RecurringExpenseLinkFailure.AlreadyLinked,
                    "The household bill is already linked to another recurring occurrence.");
            }

            occurrence.ContaCasaId = billId;
        }

        if (request.VariableExpenseId is { } variableId)
        {
            if (!await context.DespesasVariaveis.AnyAsync(item => item.Id == variableId, cancellationToken))
            {
                throw new RecurringExpenseLinkException(
                    RecurringExpenseLinkFailure.LedgerRecordNotFound,
                    "The linked variable expense was not found.");
            }

            if (await context.RecurringExpenseOccurrences.AnyAsync(item => item.DespesaVariavelId == variableId, cancellationToken))
            {
                throw new RecurringExpenseLinkException(
                    RecurringExpenseLinkFailure.AlreadyLinked,
                    "The variable expense is already linked to another recurring occurrence.");
            }

            occurrence.DespesaVariavelId = variableId;
        }

        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(occurrence);
    }

    private static void Validate(RecurringExpenseRequest request)
    {
        if (request.ChargeType is not RecurringExpense.ServiceType and not RecurringExpense.HouseholdType)
        {
            throw new ArgumentException("Charge type must be Serviço or Conta da casa.");
        }

        if (request.Amount <= 0
            || request.Cadence is not RecurringExpense.MonthlyCadence and not RecurringExpense.YearlyCadence
            || request.InitialRenewalDate == default)
        {
            throw new ArgumentException("Amount, cadence, and initial renewal date must be valid.");
        }
    }

    private static void Apply(RecurringExpense expense, RecurringExpenseRequest request)
    {
        expense.Name = request.Name.Trim();
        expense.ChargeType = request.ChargeType;
        expense.Amount = request.Amount;
        expense.PaymentType = request.PaymentType.Trim();
        expense.RenewalDay = request.InitialRenewalDate.Day;
        expense.Cadence = request.Cadence;
        expense.InitialRenewalDate = request.InitialRenewalDate.Date;
    }

    private static RecurringExpenseResponse ToResponse(RecurringExpense expense) => new(
        expense.Id,
        expense.Name,
        expense.ChargeType,
        expense.Amount,
        expense.PaymentType,
        expense.RenewalDay,
        expense.Cadence,
        expense.InitialRenewalDate);

    private static RecurringExpenseOccurrenceResponse ToResponse(RecurringExpenseOccurrence occurrence) => new(
        occurrence.Id,
        occurrence.RecurringExpenseId,
        occurrence.RecurringExpense.Name,
        occurrence.Amount,
        new DateTimeOffset(DateTime.SpecifyKind(occurrence.RenewalDate.Date, DateTimeKind.Unspecified), TimeSpan.Zero),
        occurrence.ContaCasaId,
        occurrence.DespesaVariavelId);
}