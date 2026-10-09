using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiro.API.Contracts;

/// <summary>Payload for creating or updating a bank account.</summary>
public sealed record BankAccountRequest
{
    /// <summary>The financial institution's name.</summary>
    [Required, StringLength(100)]
    public required string BankName { get; init; }

    /// <summary>The account's display name.</summary>
    [Required, StringLength(100)]
    public required string AccountName { get; init; }

    /// <summary>The account type, such as checking or savings.</summary>
    [Required, StringLength(30)]
    public required string AccountType { get; init; }

    /// <summary>The full branch identifier.</summary>
    [Required, StringLength(30)]
    public required string BranchIdentifier { get; init; }

    /// <summary>The full account identifier, including its digit when applicable.</summary>
    [Required, StringLength(30)]
    public required string AccountIdentifier { get; init; }

    /// <summary>The recorded account balance.</summary>
    [Range(-99999999.99, 99999999.99)]
    public required decimal Balance { get; init; }

    /// <summary>The portion of the overdraft currently used.</summary>
    [Range(0, 99999999.99)]
    public required decimal OverdraftUsed { get; init; }

    /// <summary>The account's overdraft limit.</summary>
    [Range(0, 99999999.99)]
    public required decimal OverdraftLimit { get; init; }
}

/// <summary>A bank account with identifiers masked for ordinary responses.</summary>
public sealed record BankAccountResponse(
    int Id,
    string BankName,
    string AccountName,
    string AccountType,
    string BranchIdentifier,
    string AccountIdentifier,
    decimal Balance,
    decimal OverdraftUsed,
    decimal OverdraftLimit,
    decimal AvailableOverdraft);

/// <summary>Payload for creating or updating a credit card.</summary>
public sealed record CreditCardRequest
{
    /// <summary>The card issuer's name.</summary>
    [Required, StringLength(100)]
    public required string Issuer { get; init; }

    /// <summary>The card's display name.</summary>
    [Required, StringLength(100)]
    public required string DisplayName { get; init; }

    /// <summary>The purchase limit.</summary>
    [Range(0, 99999999.99)]
    public required decimal PurchaseLimit { get; init; }

    /// <summary>The local cash withdrawal limit.</summary>
    [Range(0, 99999999.99)]
    public required decimal LocalCashWithdrawalLimit { get; init; }

    /// <summary>The international cash withdrawal limit.</summary>
    [Range(0, 99999999.99)]
    public required decimal InternationalCashWithdrawalLimit { get; init; }

    /// <summary>Whether credit Pix is enabled.</summary>
    public required bool CreditPixEnabled { get; init; }

    /// <summary>Whether bill payment by credit is enabled.</summary>
    public required bool BillPaymentEnabled { get; init; }

    /// <summary>The card's monthly due day, from 1 through 31.</summary>
    [Range(1, 31)]
    public required int DueDay { get; init; }
}

/// <summary>A credit card's limits and payment capabilities.</summary>
public sealed record CreditCardResponse(
    int Id,
    string Issuer,
    string DisplayName,
    decimal PurchaseLimit,
    decimal LocalCashWithdrawalLimit,
    decimal InternationalCashWithdrawalLimit,
    bool CreditPixEnabled,
    bool BillPaymentEnabled,
    int DueDay);