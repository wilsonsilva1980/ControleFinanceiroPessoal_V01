using ControleFinanceiro.API.Contracts;

namespace ControleFinanceiro.API.Services;

public interface IRecurringExpenseService
{
    Task<IReadOnlyList<RecurringExpenseResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<RecurringExpenseResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<RecurringExpenseResponse> CreateAsync(RecurringExpenseRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, RecurringExpenseRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task EnsureMonthlyProjectionAsync(int year, int month, CancellationToken cancellationToken);
    Task<RecurringExpenseProjectionResponse> GetMonthlyProjectionAsync(
        int year,
        int month,
        CancellationToken cancellationToken);
    Task<RecurringExpenseOccurrenceResponse?> LinkOccurrenceAsync(
        int occurrenceId,
        LinkRecurringExpenseOccurrenceRequest request,
        CancellationToken cancellationToken);
}