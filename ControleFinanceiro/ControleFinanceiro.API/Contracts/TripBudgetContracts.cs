using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiro.API.Contracts;

public sealed record CreateTripBudgetRequest
{
    [Required, StringLength(150)]
    public required string Name { get; init; }

    [Required, StringLength(100)]
    public required string Destination { get; init; }

    [Range(0.0001, 99999999.9999)]
    public decimal? ClpPerBrlQuote { get; init; }
}

public sealed record UpdateTripBudgetRequest
{
    [Required, StringLength(150)]
    public required string Name { get; init; }

    [Required, StringLength(100)]
    public required string Destination { get; init; }

    [Range(0.0001, 99999999.9999)]
    public decimal? ClpPerBrlQuote { get; init; }
}

public sealed record CreateTripCostItemRequest
{
    [Required, StringLength(150)]
    public required string Description { get; init; }

    [Range(0, 99999999.99)]
    public required decimal ValueBRL { get; init; }

    [Range(0, 99999999.99)]
    public required decimal FeesBRL { get; init; }

    [Range(0, 99999999.99)]
    public decimal? ValueUSD { get; init; }

    [Range(1, int.MaxValue)]
    public required int InstallmentCount { get; init; }

    public IReadOnlyList<CreateTripInstallmentRequest> Installments { get; init; } = [];
}

public sealed record CreateTripInstallmentRequest
{
    public required DateTimeOffset DueDate { get; init; }

    [Range(0.01, 99999999.99)]
    public required decimal Amount { get; init; }
}

public sealed record CreateTripPaymentRequest
{
    public required DateTimeOffset PaymentDate { get; init; }

    [Range(0.01, 99999999.99)]
    public required decimal Amount { get; init; }

    public int? TripInstallmentId { get; init; }
}

public sealed record TripBudgetResponse(
    int Id,
    string Name,
    string Destination,
    decimal? ClpPerBrlQuote,
    decimal? ClpTotal,
    decimal TotalBRL,
    decimal PaidAmount,
    decimal RemainingAmount,
    IReadOnlyList<TripCostItemResponse> Items);

public sealed record TripCostItemResponse(
    int Id,
    string Description,
    decimal ValueBRL,
    decimal FeesBRL,
    decimal? ValueUSD,
    int InstallmentCount,
    decimal TotalBRL,
    decimal PaidAmount,
    decimal RemainingAmount,
    IReadOnlyList<TripInstallmentResponse> Installments,
    IReadOnlyList<TripPaymentResponse> Payments);

public sealed record TripInstallmentResponse(
    int Id,
    DateTimeOffset DueDate,
    decimal Amount);

public sealed record TripPaymentResponse(
    int Id,
    DateTimeOffset PaymentDate,
    decimal Amount,
    int? TripInstallmentId);
