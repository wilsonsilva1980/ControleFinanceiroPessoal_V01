namespace ControleFinanceiro.API.Models;

public class ContaCasa
{
    public int Id { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal ValorPrevisto { get; set; }
    public decimal? ValorPago { get; set; }
    public string Status { get; set; } = "Pendente";
}