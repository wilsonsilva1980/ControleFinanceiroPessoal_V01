namespace ControleFinanceiro.API.Models;

public class Debt
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal? CreditLimit { get; set; }
    public ICollection<DebtInstallment> Installments { get; set; } = [];
    public ICollection<DebtEarlyPayment> EarlyPayments { get; set; } = [];
}