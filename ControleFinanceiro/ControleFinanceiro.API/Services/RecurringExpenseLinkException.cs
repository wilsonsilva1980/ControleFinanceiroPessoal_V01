namespace ControleFinanceiro.API.Services;

public enum RecurringExpenseLinkFailure
{
    InvalidCombination,
    LedgerRecordNotFound,
    AlreadyLinked
}

public sealed class RecurringExpenseLinkException(
    RecurringExpenseLinkFailure failure,
    string message) : Exception(message)
{
    public RecurringExpenseLinkFailure Failure { get; } = failure;
}