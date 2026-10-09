using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiro.API.Contracts;

/// <summary>Payload for creating or updating a recurring charge.</summary>
public sealed record RecurringExpenseRequest
{
    /// <summary>The charge's display name.</summary>
    [Required, StringLength(150)]
    public required string Name { get; init; }

    /// <summary>Either <c>Serviço</c> or <c>Conta da casa</c>.</summary>
    [Required, StringLength(30)]
    public required string ChargeType { get; init; }

    /// <summary>The recurring charge amount.</summary>
    [Range(0.01, 99999999.99)]
    public required decimal Amount { get; init; }

    /// <summary>The configured payment method.</summary>
    [Required, StringLength(50)]
    public required string PaymentType { get; init; }

    /// <summary>The configured monthly or yearly schedule.</summary>
    [Required, StringLength(20)]
    public required string Cadence { get; init; }

    /// <summary>The first scheduled renewal date.</summary>
    public required DateTime InitialRenewalDate { get; init; }
}

/// <summary>A recurring charge definition.</summary>
public sealed record RecurringExpenseResponse(
    int Id,
    string Name,
    string ChargeType,
    decimal Amount,
    string PaymentType,
    int RenewalDay,
    string Cadence,
    DateTime InitialRenewalDate);

/// <summary>A dated occurrence of a recurring charge.</summary>
public sealed record RecurringExpenseOccurrenceResponse(
    int Id,
    int RecurringExpenseId,
    string Name,
    decimal Amount,
    DateTimeOffset RenewalDate,
    int? HouseBillId,
    int? VariableExpenseId);

/// <summary>Recurring charges projected for one calendar month.</summary>
public sealed record RecurringExpenseProjectionResponse(
    int Year,
    int Month,
    IReadOnlyList<RecurringExpenseOccurrenceResponse> Occurrences,
    decimal ScheduledAmount,
    decimal AdditionalCashOutflowAmount);

/// <summary>Links an occurrence to one equivalent existing expense.</summary>
public sealed record LinkRecurringExpenseOccurrenceRequest
{
    /// <summary>The equivalent household bill, when applicable.</summary>
    public int? HouseBillId { get; init; }

    /// <summary>The equivalent variable expense, when applicable.</summary>
    public int? VariableExpenseId { get; init; }
}