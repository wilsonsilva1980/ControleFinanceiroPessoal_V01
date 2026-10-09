using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Services;

public sealed class FinancialAccountService(AppDbContext context) : IFinancialAccountService
{
    public async Task<IReadOnlyList<BankAccountResponse>> GetBankAccountsAsync(CancellationToken cancellationToken) =>
        (await context.BankAccounts.AsNoTracking().OrderBy(item => item.BankName).ToListAsync(cancellationToken))
        .Select(ToResponse)
        .ToArray();

    public async Task<BankAccountResponse?> GetBankAccountAsync(int id, CancellationToken cancellationToken)
    {
        var account = await context.BankAccounts.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return account is null ? null : ToResponse(account);
    }

    public async Task<BankAccountResponse> CreateBankAccountAsync(
        BankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = new BankAccount();
        Apply(account, request);
        context.BankAccounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(account);
    }

    public async Task<bool> UpdateBankAccountAsync(
        int id,
        BankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await context.BankAccounts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (account is null) return false;
        Apply(account, request);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteBankAccountAsync(int id, CancellationToken cancellationToken)
    {
        var account = await context.BankAccounts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (account is null) return false;
        context.BankAccounts.Remove(account);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<CreditCardResponse>> GetCreditCardsAsync(CancellationToken cancellationToken) =>
        (await context.CreditCards.AsNoTracking().OrderBy(item => item.Issuer).ThenBy(item => item.DisplayName).ToListAsync(cancellationToken))
        .Select(ToResponse)
        .ToArray();

    public async Task<CreditCardResponse?> GetCreditCardAsync(int id, CancellationToken cancellationToken)
    {
        var card = await context.CreditCards.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return card is null ? null : ToResponse(card);
    }

    public async Task<CreditCardResponse> CreateCreditCardAsync(
        CreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var card = new CreditCard();
        Apply(card, request);
        context.CreditCards.Add(card);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(card);
    }

    public async Task<bool> UpdateCreditCardAsync(
        int id,
        CreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var card = await context.CreditCards.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (card is null) return false;
        Apply(card, request);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCreditCardAsync(int id, CancellationToken cancellationToken)
    {
        var card = await context.CreditCards.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (card is null) return false;
        context.CreditCards.Remove(card);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(BankAccount account, BankAccountRequest request)
    {
        account.BankName = request.BankName.Trim();
        account.AccountName = request.AccountName.Trim();
        account.AccountType = request.AccountType.Trim();
        account.BranchIdentifier = request.BranchIdentifier.Trim();
        account.AccountIdentifier = request.AccountIdentifier.Trim();
        account.Balance = request.Balance;
        account.OverdraftUsed = request.OverdraftUsed;
        account.OverdraftLimit = request.OverdraftLimit;
    }

    private static void Apply(CreditCard card, CreditCardRequest request)
    {
        card.Issuer = request.Issuer.Trim();
        card.DisplayName = request.DisplayName.Trim();
        card.PurchaseLimit = request.PurchaseLimit;
        card.LocalCashWithdrawalLimit = request.LocalCashWithdrawalLimit;
        card.InternationalCashWithdrawalLimit = request.InternationalCashWithdrawalLimit;
        card.CreditPixEnabled = request.CreditPixEnabled;
        card.BillPaymentEnabled = request.BillPaymentEnabled;
        card.DueDay = request.DueDay;
    }

    private static BankAccountResponse ToResponse(BankAccount account) => new(
        account.Id,
        account.BankName,
        account.AccountName,
        account.AccountType,
        MaskIdentifier(account.BranchIdentifier, 2),
        MaskIdentifier(account.AccountIdentifier, 4),
        account.Balance,
        account.OverdraftUsed,
        account.OverdraftLimit,
        account.AvailableOverdraft);

    private static CreditCardResponse ToResponse(CreditCard card) => new(
        card.Id,
        card.Issuer,
        card.DisplayName,
        card.PurchaseLimit,
        card.LocalCashWithdrawalLimit,
        card.InternationalCashWithdrawalLimit,
        card.CreditPixEnabled,
        card.BillPaymentEnabled,
        card.DueDay);

    private static string MaskIdentifier(string identifier, int visibleCharacters) =>
        identifier.Length <= visibleCharacters
            ? new string('*', identifier.Length)
            : $"{new string('*', identifier.Length - visibleCharacters)}{identifier[^visibleCharacters..]}";
}