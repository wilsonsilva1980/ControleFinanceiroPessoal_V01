namespace ControleFinanceiro.API.Models;

public class CartaoLancamento
{
    public int Id { get; set; }
    public DateTime DataCompra { get; set; }
    public string Estabelecimento { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int NumeroParcela { get; set; } = 1;
    public int TotalParcelas { get; set; } = 1;
    public decimal ValorParcela { get; set; }
    public string MesFatura { get; set; } = string.Empty;
    public int AnoFatura { get; set; }
}