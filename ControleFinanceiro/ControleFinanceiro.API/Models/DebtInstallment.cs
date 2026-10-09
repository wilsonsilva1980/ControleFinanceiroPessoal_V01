namespace ControleFinanceiro.API.Models;

public class DebtInstallment
{
    public const string PendingStatus = "Pendente";
    public const string PaidStatus = "Pago";

    public int Id { get; set; }
    public int DebtId { get; set; }
    public Debt Debt { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public decimal ScheduledAmount { get; set; }
    public string Status { get; set; } = PendingStatus;
    public DateTime? PaymentDate { get; set; }
    public DebtOutflowLink? OutflowLink { get; set; }
    public decimal PaidAmount => Status == PaidStatus ? ScheduledAmount : 0m;
    public decimal RemainingAmount => Status == PendingStatus ? ScheduledAmount : 0m;
}