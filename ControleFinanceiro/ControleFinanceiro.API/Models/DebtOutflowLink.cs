namespace ControleFinanceiro.API.Models;

public class DebtOutflowLink
{
    public int Id { get; set; }
    public int? DebtInstallmentId { get; set; }
    public DebtInstallment? DebtInstallment { get; set; }
    public int? DebtEarlyPaymentId { get; set; }
    public DebtEarlyPayment? DebtEarlyPayment { get; set; }
    public int? ContaCasaId { get; set; }
    public int? CartaoLancamentoId { get; set; }
}