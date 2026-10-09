using ControleFinanceiro.API.Contracts;

namespace ControleFinanceiro.API.Services;

public interface ITripBudgetService
{
    Task<IReadOnlyList<TripBudgetResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<TripBudgetResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TripBudgetResponse> CreateAsync(CreateTripBudgetRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, UpdateTripBudgetRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<TripCostItemResponse?> CreateItemAsync(
        int budgetId,
        CreateTripCostItemRequest request,
        CancellationToken cancellationToken);
    Task<TripPaymentResponse?> CreatePaymentAsync(
        int costItemId,
        CreateTripPaymentRequest request,
        CancellationToken cancellationToken);
}
