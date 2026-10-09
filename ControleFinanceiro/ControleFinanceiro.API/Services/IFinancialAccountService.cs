using ControleFinanceiro.API.Contracts;

namespace ControleFinanceiro.API.Services;

public interface IFinancialAccountService
{
    Task<IReadOnlyList<BankAccountResponse>> GetBankAccountsAsync(CancellationToken cancellationToken);
    Task<BankAccountResponse?> GetBankAccountAsync(int id, CancellationToken cancellationToken);
    Task<BankAccountResponse> CreateBankAccountAsync(BankAccountRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateBankAccountAsync(int id, BankAccountRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteBankAccountAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CreditCardResponse>> GetCreditCardsAsync(CancellationToken cancellationToken);
    Task<CreditCardResponse?> GetCreditCardAsync(int id, CancellationToken cancellationToken);
    Task<CreditCardResponse> CreateCreditCardAsync(CreditCardRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateCreditCardAsync(int id, CreditCardRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteCreditCardAsync(int id, CancellationToken cancellationToken);
}