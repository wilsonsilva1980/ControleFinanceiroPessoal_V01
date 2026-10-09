namespace ControleFinanceiro.API.Models;

public class RecurringExpenseOccurrence
{
    public int Id { get; set; }
    public int RecurringExpenseId { get; set; }
    public RecurringExpense RecurringExpense { get; set; } = null!;
    public DateTime RenewalDate { get; set; }
    public decimal Amount { get; set; }
    public int? ContaCasaId { get; set; }
    public int? DespesaVariavelId { get; set; }
}