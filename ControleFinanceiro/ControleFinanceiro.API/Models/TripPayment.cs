namespace ControleFinanceiro.API.Models;

public class TripPayment
{
    public int Id { get; set; }
    public int TripCostItemId { get; set; }
    public TripCostItem TripCostItem { get; set; } = null!;
    public int? TripInstallmentId { get; set; }
    public TripInstallment? TripInstallment { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
}
