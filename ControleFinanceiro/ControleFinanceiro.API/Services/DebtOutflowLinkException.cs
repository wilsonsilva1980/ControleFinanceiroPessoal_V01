namespace ControleFinanceiro.API.Services;

public enum DebtOutflowLinkFailure
{
    InvalidCombination,
    LedgerRecordNotFound,
    AlreadyLinked
}

public sealed class DebtOutflowLinkException(
    DebtOutflowLinkFailure failure,
    string message) : Exception(message)
{
    public DebtOutflowLinkFailure Failure { get; } = failure;
}