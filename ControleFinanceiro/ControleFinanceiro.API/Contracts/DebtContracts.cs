using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiro.API.Contracts;

/// <summary>Payload for creating a debt and its dated installments.</summary>
public sealed record CreateDebtRequest
{
    /// <summary>The debt's display description.</summary>
    [Required, StringLength(150)]
    public required string Description { get; init; }

    /// <summary>The debt's due date.</summary>
    public required DateTimeOffset DueDate { get; init; }

    /// <summary>The original amount owed.</summary>
    [Range(0.01, 99999999.99)]
    public required decimal OriginalAmount { get; init; }

    /// <summary>The applicable credit limit, when one exists.</summary>
    [Range(0, 99999999.99)]
    public decimal? CreditLimit { get; init; }

    /// <summary>The dated installments that make up the schedule.</summary>
    [Required, MinLength(1)]
    public required IReadOnlyList<CreateDebtInstallmentRequest> Installments { get; init; }
}

/// <summary>One scheduled installment included when a debt is created.</summary>
public sealed record CreateDebtInstallmentRequest
{
    /// <summary>The installment's due date.</summary>
    public required DateTimeOffset DueDate { get; init; }

    /// <summary>The amount scheduled for this installment.</summary>
    [Range(0.01, 99999999.99)]
    public required decimal ScheduledAmount { get; init; }

    /// <summary>The existing household bill represented by this installment.</summary>
    public int? LinkedHouseBillId { get; init; }

    /// <summary>The existing card purchase represented by this installment.</summary>
    public int? LinkedCardPurchaseId { get; init; }
}

/// <summary>Updates an installment's payment state.</summary>
public sealed record UpdateDebtInstallmentPaymentRequest
{
    /// <summary>Either <c>Pendente</c> or <c>Pago</c>.</summary>
    [Required]
    public required string Status { get; init; }

    /// <summary>The payment date required when status is <c>Pago</c>.</summary>
    public DateTimeOffset? PaymentDate { get; init; }
}

/// <summary>Payload for recording an additional early debt payment.</summary>
public sealed record CreateDebtEarlyPaymentRequest
{
    /// <summary>The early payment's date.</summary>
    public required DateTimeOffset PaymentDate { get; init; }

    /// <summary>The amount paid early.</summary>
    [Range(0.01, 99999999.99)]
    public required decimal Amount { get; init; }

    /// <summary>The existing household bill represented by this payment.</summary>
    public int? LinkedHouseBillId { get; init; }

    /// <summary>The existing card purchase represented by this payment.</summary>
    public int? LinkedCardPurchaseId { get; init; }
}

/// <summary>A debt and its scheduled installments and early payments.</summary>
public sealed record DebtResponse(
    int Id,
    string Description,
    DateTimeOffset DueDate,
    decimal OriginalAmount,
    decimal? CreditLimit,
    IReadOnlyList<DebtInstallmentResponse> Installments,
    IReadOnlyList<DebtEarlyPaymentResponse> EarlyPayments);

/// <summary>A scheduled debt installment with derived payment amounts.</summary>
public sealed record DebtInstallmentResponse(
    int Id,
    DateTimeOffset DueDate,
    decimal ScheduledAmount,
    string Status,
    DateTimeOffset? PaymentDate,
    decimal PaidAmount,
    decimal RemainingAmount,
    int? LinkedHouseBillId,
    int? LinkedCardPurchaseId);

/// <summary>An additional payment recorded against a debt.</summary>
public sealed record DebtEarlyPaymentResponse(
    int Id,
    DateTimeOffset PaymentDate,
    decimal Amount,
    int? LinkedHouseBillId,
    int? LinkedCardPurchaseId);

/// <summary>Debt totals and cash outflow for a calendar period.</summary>
public sealed record DebtPeriodSummaryResponse(
    int Year,
    int? Month,
    decimal ScheduledAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    decimal CashOutflowAmount);

/// <summary>Aggregated preview counts for a workbook upload.</summary>
public sealed record DebtWorkbookPreviewResponse(
    int DebtCount,
    int InstallmentCount,
    int UndatedEarlyPaymentsSkipped);

/// <summary>Import result after confirming a workbook upload.</summary>
public sealed record DebtWorkbookImportResponse(
    Guid? BatchId,
    int DebtsInserted,
    int InstallmentsInserted,
    int AlreadyImported,
    int UndatedEarlyPaymentsSkipped);