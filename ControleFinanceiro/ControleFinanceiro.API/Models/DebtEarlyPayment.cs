namespace ControleFinanceiro.API.Models;

public class DebtEarlyPayment
{
    public int Id { get; set; }
    public int DebtId { get; set; }
    public Debt Debt { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public DebtOutflowLink? OutflowLink { get; set; }
}