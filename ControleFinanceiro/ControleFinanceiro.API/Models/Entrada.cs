namespace ControleFinanceiro.API.Models;

public class Entrada
{
    public int Id { get; set; }
    public DateTime DataPrevista { get; set; }
    public DateTime? DataRecebimento { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal ValorPrevisto { get; set; }
    public decimal? ValorRecebido { get; set; }
    public string Status { get; set; } = "Pendente";
}