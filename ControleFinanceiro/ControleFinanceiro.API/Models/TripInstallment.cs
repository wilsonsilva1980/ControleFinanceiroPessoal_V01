namespace ControleFinanceiro.API.Models;

public class TripInstallment
{
    public int Id { get; set; }
    public int TripCostItemId { get; set; }
    public TripCostItem TripCostItem { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public ICollection<TripPayment> Payments { get; set; } = [];
}
