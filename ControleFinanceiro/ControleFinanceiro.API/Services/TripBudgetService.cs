using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Services;

public sealed class TripBudgetService(AppDbContext context) : ITripBudgetService
{
    public async Task<IReadOnlyList<TripBudgetResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var budgets = await QueryBudgets()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return budgets.Select(ToResponse).ToArray();
    }

    public async Task<TripBudgetResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var budget = await QueryBudgets().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return budget is null ? null : ToResponse(budget);
    }

    public async Task<TripBudgetResponse> CreateAsync(
        CreateTripBudgetRequest request,
        CancellationToken cancellationToken)
    {
        ValidateBudget(request.Name, request.Destination, request.ClpPerBrlQuote);
        var budget = new TripBudget
        {
            Name = request.Name.Trim(),
            Destination = request.Destination.Trim(),
            ClpPerBrlQuote = request.ClpPerBrlQuote
        };
        context.TripBudgets.Add(budget);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(budget);
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateTripBudgetRequest request,
        CancellationToken cancellationToken)
    {
        ValidateBudget(request.Name, request.Destination, request.ClpPerBrlQuote);
        var budget = await context.TripBudgets.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (budget is null) return false;
        budget.Name = request.Name.Trim();
        budget.Destination = request.Destination.Trim();
        budget.ClpPerBrlQuote = request.ClpPerBrlQuote;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var budget = await context.TripBudgets
            .Include(item => item.Items)
            .ThenInclude(item => item.Installments)
            .Include(item => item.Items)
            .ThenInclude(item => item.Payments)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (budget is null) return false;
        context.TripBudgets.Remove(budget);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TripCostItemResponse?> CreateItemAsync(
        int budgetId,
        CreateTripCostItemRequest request,
        CancellationToken cancellationToken)
    {
        ValidateItem(request);
        if (!await context.TripBudgets.AnyAsync(item => item.Id == budgetId, cancellationToken))
        {
            return null;
        }

        var item = new TripCostItem
        {
            TripBudgetId = budgetId,
            Description = request.Description.Trim(),
            ValueBRL = request.ValueBRL,
            FeesBRL = request.FeesBRL,
            ValueUSD = request.ValueUSD,
            InstallmentCount = request.InstallmentCount,
            Installments = request.Installments.Select(installment => new TripInstallment
            {
                DueDate = installment.DueDate.Date,
                Amount = installment.Amount
            }).ToArray()
        };
        context.TripCostItems.Add(item);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(item);
    }

    public async Task<TripPaymentResponse?> CreatePaymentAsync(
        int costItemId,
        CreateTripPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0 || request.PaymentDate == default)
        {
            throw new ArgumentException("Payment amount and date must be valid.");
        }

        var item = await context.TripCostItems.SingleOrDefaultAsync(
            costItem => costItem.Id == costItemId,
            cancellationToken);
        if (item is null) return null;

        if (request.TripInstallmentId is { } installmentId
            && !await context.TripInstallments.AnyAsync(
                installment => installment.Id == installmentId && installment.TripCostItemId == costItemId,
                cancellationToken))
        {
            throw new ArgumentException("The installment does not belong to this trip item.");
        }

        var payment = new TripPayment
        {
            TripCostItemId = costItemId,
            TripInstallmentId = request.TripInstallmentId,
            PaymentDate = request.PaymentDate.Date,
            Amount = request.Amount
        };
        context.TripPayments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(payment);
    }

    private IQueryable<TripBudget> QueryBudgets() =>
        context.TripBudgets
            .AsNoTracking()
            .Include(item => item.Items)
            .ThenInclude(item => item.Installments)
            .Include(item => item.Items)
            .ThenInclude(item => item.Payments);

    private static void ValidateBudget(string name, string destination, decimal? quote)
    {
        if (string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(destination)
            || (quote.HasValue && quote <= 0))
        {
            throw new ArgumentException("Name, destination, and a positive optional CLP quote are required.");
        }
    }

    private static void ValidateItem(CreateTripCostItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description)
            || request.ValueBRL < 0
            || request.FeesBRL < 0
            || request.ValueUSD < 0
            || request.InstallmentCount < 1
            || (request.Installments.Count > 0 && request.Installments.Count != request.InstallmentCount)
            || (request.Installments.Count > 0
                && request.Installments.Sum(item => item.Amount) != request.ValueBRL + request.FeesBRL)
            || request.Installments.Any(item => item.DueDate == default || item.Amount <= 0))
        {
            throw new ArgumentException("Trip item amounts and installments must be valid and scheduled amounts must equal the total.");
        }
    }

    private static TripBudgetResponse ToResponse(TripBudget budget) => new(
        budget.Id,
        budget.Name,
        budget.Destination,
        budget.ClpPerBrlQuote,
        budget.ClpTotal,
        budget.TotalBRL,
        budget.PaidAmount,
        budget.RemainingAmount,
        budget.Items.Select(ToResponse).ToArray());

    private static TripCostItemResponse ToResponse(TripCostItem item) => new(
        item.Id,
        item.Description,
        item.ValueBRL,
        item.FeesBRL,
        item.ValueUSD,
        item.InstallmentCount,
        item.TotalBRL,
        item.PaidAmount,
        item.RemainingAmount,
        item.Installments
            .OrderBy(installment => installment.DueDate)
            .Select(installment => new TripInstallmentResponse(
                installment.Id,
                ToDateOffset(installment.DueDate),
                installment.Amount))
            .ToArray(),
        item.Payments
            .OrderBy(payment => payment.PaymentDate)
            .Select(ToResponse)
            .ToArray());

    private static TripPaymentResponse ToResponse(TripPayment payment) => new(
        payment.Id,
        ToDateOffset(payment.PaymentDate),
        payment.Amount,
        payment.TripInstallmentId);

    private static DateTimeOffset ToDateOffset(DateTime date) =>
        new(DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified), TimeSpan.Zero);
}
