namespace ControleFinanceiro.API.Models;

public class BankAccount
{
    public int Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string BranchIdentifier { get; set; } = string.Empty;
    public string AccountIdentifier { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal OverdraftUsed { get; set; }
    public decimal OverdraftLimit { get; set; }
    public decimal AvailableOverdraft => OverdraftLimit - OverdraftUsed;
}