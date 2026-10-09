namespace ControleFinanceiro.API.Models;

public class CreditCard
{
    public int Id { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal PurchaseLimit { get; set; }
    public decimal LocalCashWithdrawalLimit { get; set; }
    public decimal InternationalCashWithdrawalLimit { get; set; }
    public bool CreditPixEnabled { get; set; }
    public bool BillPaymentEnabled { get; set; }
    public int DueDay { get; set; }
}