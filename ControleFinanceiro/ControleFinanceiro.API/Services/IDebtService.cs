using ControleFinanceiro.API.Contracts;

namespace ControleFinanceiro.API.Services;

public interface IDebtService
{
    Task<IReadOnlyList<DebtResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<DebtResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<DebtResponse> CreateAsync(CreateDebtRequest request, CancellationToken cancellationToken);
    Task<DebtInstallmentResponse?> UpdateInstallmentPaymentAsync(
        int installmentId,
        UpdateDebtInstallmentPaymentRequest request,
        CancellationToken cancellationToken);
    Task<DebtEarlyPaymentResponse?> CreateEarlyPaymentAsync(
        int debtId,
        CreateDebtEarlyPaymentRequest request,
        CancellationToken cancellationToken);
    Task<DebtPeriodSummaryResponse> GetPeriodSummaryAsync(
        int year,
        int? month,
        CancellationToken cancellationToken);
}