namespace ControleFinanceiro.API.Models;

public class TripBudget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal? ClpPerBrlQuote { get; set; }
    public ICollection<TripCostItem> Items { get; set; } = [];
    public decimal TotalBRL => Items.Sum(item => item.TotalBRL);
    public decimal PaidAmount => Items.Sum(item => item.PaidAmount);
    public decimal RemainingAmount => TotalBRL - PaidAmount;
    public decimal? ClpTotal => ClpPerBrlQuote is { } quote ? TotalBRL * quote : null;
}
