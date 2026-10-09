namespace ControleFinanceiro.API.Models;

public class RecurringExpense
{
    public const string ServiceType = "Serviço";
    public const string HouseholdType = "Conta da casa";
    public const string MonthlyCadence = "Monthly";
    public const string YearlyCadence = "Yearly";

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ChargeType { get; set; } = ServiceType;
    public decimal Amount { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public int RenewalDay { get; set; }
    public string Cadence { get; set; } = MonthlyCadence;
    public DateTime InitialRenewalDate { get; set; }
    public ICollection<RecurringExpenseOccurrence> Occurrences { get; set; } = [];

    public DateTime? GetRenewalDate(int year, int month)
    {
        if (year is < 1 or > 9998 || month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Provide a valid year and month.");
        }

        if (InitialRenewalDate == default)
        {
            throw new InvalidOperationException("An initial renewal date is required.");
        }

        if (Cadence == YearlyCadence)
        {
            if (month != InitialRenewalDate.Month || year < InitialRenewalDate.Year)
            {
                return null;
            }
        }
        else if (Cadence != MonthlyCadence)
        {
            throw new InvalidOperationException("Cadence must be Monthly or Yearly.");
        }
        else if (year < InitialRenewalDate.Year
            || (year == InitialRenewalDate.Year && month < InitialRenewalDate.Month))
        {
            return null;
        }

        var clampedDay = Math.Min(InitialRenewalDate.Day, DateTime.DaysInMonth(year, month));
        return new DateTime(year, month, clampedDay);
    }
}