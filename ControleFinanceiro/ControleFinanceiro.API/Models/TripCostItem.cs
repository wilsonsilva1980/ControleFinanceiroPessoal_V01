namespace ControleFinanceiro.API.Models;

public class TripCostItem
{
    public int Id { get; set; }
    public int TripBudgetId { get; set; }
    public TripBudget TripBudget { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal ValueBRL { get; set; }
    public decimal FeesBRL { get; set; }
    public decimal? ValueUSD { get; set; }
    public int InstallmentCount { get; set; }
    public ICollection<TripInstallment> Installments { get; set; } = [];
    public ICollection<TripPayment> Payments { get; set; } = [];
    public decimal TotalBRL => ValueBRL + FeesBRL;
    public decimal PaidAmount => Payments.Sum(payment => payment.Amount);
    public decimal RemainingAmount => TotalBRL - PaidAmount;
}
