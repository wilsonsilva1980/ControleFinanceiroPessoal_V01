using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;

namespace ControleFinanceiro.DebtImport;

public sealed record DebtWorkbookImportPlan(
    IReadOnlyList<DebtWorkbookDebt> Debts,
    int UndatedEarlyPaymentsSkipped);

public sealed record DebtWorkbookDebt(
    string SourceKey,
    string Description,
    DateTime FirstDueDate,
    decimal OriginalValue,
    int InstallmentCount,
    IReadOnlyList<DebtWorkbookInstallment> Installments);

public sealed record DebtWorkbookInstallment(
    string SourceKey,
    int Number,
    DateTime DueDate,
    decimal Amount,
    string Status,
    DateTime? PaymentDate);

public sealed class DebtWorkbookParser
{
    private static readonly (string SheetName, int Year)[] ScheduleSheets =
    [
        ("Controle Pagamentos - 2024", 2024),
        ("Controle Pagamentos - 2025", 2025),
        ("Controle Pagamentos - 2026", 2026)
    ];

    private const string EarlyPaymentSheet = "CP - 2026 - Antecipação de Parc";

    public DebtWorkbookImportPlan Parse(string workbookPath)
    {
        if (!File.Exists(workbookPath))
        {
            throw new FileNotFoundException("Debt workbook was not found.", workbookPath);
        }

        using var workbookStream = File.OpenRead(workbookPath);
        using var workbook = new XLWorkbook(workbookStream);
        var debts = new Dictionary<string, DebtAccumulator>(StringComparer.Ordinal);

        foreach (var (sheetName, year) in ScheduleSheets)
        {
            if (!workbook.TryGetWorksheet(sheetName, out var sheet))
            {
                throw new InvalidDataException($"Required workbook sheet is missing: '{sheetName}'.");
            }

            ParseScheduleSheet(sheet, year, debts);
        }

        if (!workbook.TryGetWorksheet(EarlyPaymentSheet, out var earlySheet))
        {
            throw new InvalidDataException($"Required workbook sheet is missing: '{EarlyPaymentSheet}'.");
        }

        var undatedEarlyPayments = CountUndatedEarlyPayments(earlySheet);

        var result = debts.Values
            .Where(debt => debt.Installments.Count > 0)
            .Select(debt =>
            {
                var installments = debt.Installments.Values
                    .OrderBy(installment => installment.DueDate)
                    .ThenBy(installment => installment.Number)
                    .ToArray();
                return new DebtWorkbookDebt(
                    debt.SourceKey,
                    debt.Description,
                    installments[0].DueDate,
                    installments.Sum(installment => installment.Amount),
                    debt.InstallmentCount,
                    installments);
            })
            .OrderBy(debt => debt.FirstDueDate)
            .ThenBy(debt => debt.SourceKey, StringComparer.Ordinal)
            .ToArray();

        return new DebtWorkbookImportPlan(result, undatedEarlyPayments);
    }

    private static void ParseScheduleSheet(
        IXLWorksheet sheet,
        int year,
        IDictionary<string, DebtAccumulator> debts)
    {
        var months = GetMonths(sheet);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 4;
        var sourceKeysInSheet = new HashSet<string>(StringComparer.Ordinal);

        for (var row = 5; row <= lastRow; row++)
        {
            var description = sheet.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(description) || NormalizeLabel(description).StartsWith("TOTAL", StringComparison.Ordinal))
            {
                continue;
            }

            if (description.Length > 150)
            {
                throw InvalidCell(sheet.Name, row, "description length");
            }

            var installmentCount = ReadOptionalInt(sheet.Cell(row, 3));
            var dueDay = ReadOptionalInt(sheet.Cell(row, 4));
            if (installmentCount is null || dueDay is null)
            {
                if (months.Any(month => ReadOptionalInt(sheet.Cell(row, month.Columns["PARCELA"])) is > 0))
                {
                    throw InvalidCell(sheet.Name, row, "installment metadata");
                }

                continue;
            }

            if (installmentCount <= 0 || dueDay is < 1 or > 31)
            {
                throw InvalidCell(sheet.Name, row, "installment count or due day");
            }

            var sourceKey = CreateDebtKey(description, sheet.Cell(row, 2), installmentCount.Value, dueDay.Value);
            if (!sourceKeysInSheet.Add(sourceKey))
            {
                throw InvalidCell(sheet.Name, row, "ambiguous duplicate debt identity");
            }

            if (!debts.TryGetValue(sourceKey, out var debt))
            {
                debt = new DebtAccumulator(sourceKey, description, installmentCount.Value, dueDay.Value);
                debts.Add(sourceKey, debt);
            }
            else if (!string.Equals(debt.Description, description, StringComparison.Ordinal)
                     || debt.InstallmentCount != installmentCount.Value
                     || debt.DueDay != dueDay.Value)
            {
                throw InvalidCell(sheet.Name, row, "debt identity");
            }

            foreach (var month in months)
            {
                var number = ReadOptionalInt(sheet.Cell(row, month.Columns["PARCELA"]));
                var amount = ReadDecimal(sheet.Cell(row, month.Columns["VALOR"]));
                if (amount < 0)
                {
                    throw InvalidCell(sheet.Name, row, "installment amount");
                }

                amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
                if (amount == 0)
                {
                    continue;
                }

                var installmentNumber = number.GetValueOrDefault();
                if (installmentNumber < 0 || installmentNumber > installmentCount)
                {
                    throw InvalidCell(sheet.Name, row, "installment number");
                }

                var dueDate = CreateDueDate(year, month.Number, dueDay.Value);
                var paid = ReadDecimal(sheet.Cell(row, month.Columns["VALOR PAGO"])) > 0;
                var statusText = month.Columns.TryGetValue("STATUS", out var statusColumn)
                    ? sheet.Cell(row, statusColumn).GetString()
                    : string.Empty;
                var status = NormalizeStatus(statusText, paid, sheet.Name, row);
                var installmentSourceKey = Hash($"{sourceKey}|{dueDate:yyyy-MM-dd}|{installmentNumber}");
                var installment = new DebtWorkbookInstallment(
                    installmentSourceKey,
                    installmentNumber,
                    dueDate,
                    amount,
                    status,
                    PaymentDate: null);

                if (debt.Installments.TryGetValue(installmentSourceKey, out var existing))
                {
                    if (existing.Amount != installment.Amount || existing.Status != installment.Status)
                    {
                        throw InvalidCell(sheet.Name, row, "conflicting duplicate installment");
                    }
                }
                else
                {
                    debt.Installments.Add(installmentSourceKey, installment);
                }
            }
        }
    }

    private static int CountUndatedEarlyPayments(IXLWorksheet sheet)
    {
        var months = GetMonths(sheet);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 4;
        var count = 0;

        for (var row = 5; row <= lastRow; row++)
        {
            var description = NormalizeLabel(sheet.Cell(row, 1).GetString());
            if (string.IsNullOrWhiteSpace(description) || description.StartsWith("TOTAL", StringComparison.Ordinal))
            {
                continue;
            }

            var installmentCount = ReadOptionalInt(sheet.Cell(row, 3));
            var dueDay = ReadOptionalInt(sheet.Cell(row, 4));
            foreach (var month in months)
            {
                if (month.Columns.TryGetValue("QUITACAO PAGO", out var paidColumn)
                    && ReadDecimal(sheet.Cell(row, paidColumn)) > 0)
                {
                    if (installmentCount is null or <= 0 || dueDay is < 1 or > 31)
                    {
                        throw InvalidCell(sheet.Name, row, "early-payment metadata");
                    }

                    count++;
                }
            }
        }

        return count;
    }

    private static IReadOnlyList<MonthColumns> GetMonths(IXLWorksheet sheet)
    {
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var monthStarts = new List<(int Number, int Column)>();
        for (var column = 1; column <= lastColumn; column++)
        {
            var monthName = NormalizeLabel(sheet.Cell(3, column).GetString());
            var monthNumber = MonthNumber(monthName);
            if (monthNumber > 0)
            {
                monthStarts.Add((monthNumber, column));
            }
        }

        if (monthStarts.Count != 12)
        {
            throw new InvalidDataException($"Workbook sheet '{sheet.Name}' must contain all twelve month groups.");
        }

        var months = new List<MonthColumns>(12);
        for (var index = 0; index < monthStarts.Count; index++)
        {
            var (number, start) = monthStarts[index];
            var end = index + 1 < monthStarts.Count ? monthStarts[index + 1].Column : lastColumn + 1;
            var columns = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var column = start; column < end; column++)
            {
                var header = NormalizeLabel(sheet.Cell(4, column).GetString());
                if (!string.IsNullOrWhiteSpace(header))
                {
                    columns.TryAdd(header, column);
                }
            }

            if (!columns.ContainsKey("PARCELA") || !columns.ContainsKey("VALOR"))
            {
                throw new InvalidDataException($"Workbook sheet '{sheet.Name}' has an invalid month group.");
            }

            if (!columns.ContainsKey("VALOR PAGO"))
            {
                columns["VALOR PAGO"] = columns["VALOR"];
            }

            months.Add(new MonthColumns(number, columns));
        }

        return months;
    }

    private static int MonthNumber(string label) => label switch
    {
        "JANEIRO" => 1,
        "FEVEREIRO" => 2,
        "MARCO" => 3,
        "ABRIL" => 4,
        "MAIO" => 5,
        "JUNHO" => 6,
        "JULHO" => 7,
        "AGOSTO" => 8,
        "SETEMBRO" => 9,
        "OUTUBRO" => 10,
        "NOVEMBRO" => 11,
        "DEZEMBRO" => 12,
        _ => 0
    };

    private static string NormalizeStatus(string status, bool paid, string sheetName, int row)
    {
        var normalized = NormalizeLabel(status);
        if (paid || normalized is "P" or "PAGO")
        {
            return "Pago";
        }

        if (string.IsNullOrWhiteSpace(normalized)
            || normalized is "PENDENTE" or "EM ABERTO" or "NAO PAGO")
        {
            return "Pendente";
        }

        throw InvalidCell(sheetName, row, "status");
    }

    private static DateTime CreateDueDate(int year, int month, int day)
    {
        return new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
    }

    private static int? ReadOptionalInt(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        var value = ReadDecimal(cell);
        if (value != decimal.Truncate(value) || value is < int.MinValue or > int.MaxValue)
        {
            throw new InvalidDataException("Workbook contains an invalid integer field.");
        }

        return (int)value;
    }

    private static decimal ReadDecimal(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return 0m;
        }

        if (cell.DataType == XLDataType.Number)
        {
            var numericText = cell.GetDouble().ToString("R", CultureInfo.InvariantCulture);
            if (decimal.TryParse(numericText, NumberStyles.Float, CultureInfo.InvariantCulture, out var numericAmount))
            {
                return numericAmount;
            }

            throw new InvalidDataException("Workbook contains an out-of-range numeric value.");
        }

        if (cell.DataType != XLDataType.Text)
        {
            throw new InvalidDataException("Workbook contains a non-numeric amount.");
        }

        var text = cell.GetString()
            .Trim()
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\u00a0", string.Empty, StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0m;
        }

        if (text.Contains(','))
        {
            text = text.Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace(',', '.');
        }
        else if (HasBrazilianThousandsSeparators(text))
        {
            text = text.Replace(".", string.Empty, StringComparison.Ordinal);
        }

        if (decimal.TryParse(
                text,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            return amount;
        }

        throw new InvalidDataException("Workbook contains an invalid amount.");
    }

    private static bool HasBrazilianThousandsSeparators(string text)
    {
        var unsigned = text.StartsWith("-", StringComparison.Ordinal) ? text[1..] : text;
        var groups = unsigned.Split('.');
        return groups.Length > 1
            && groups[0].Length is > 0 and <= 3
            && groups.Skip(1).All(group => group.Length == 3 && group.All(char.IsDigit));
    }

    private static string CreateDebtKey(string description, IXLCell purchaseDate, int installmentCount, int dueDay)
    {
        var purchaseMarker = purchaseDate.IsEmpty()
            ? $"missing:{installmentCount}:{dueDay}"
            : purchaseDate.DataType == XLDataType.DateTime
                ? purchaseDate.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : NormalizeLabel(purchaseDate.Value.ToString(CultureInfo.InvariantCulture));
        var normalizedDescription = string.Join(
            ' ',
            description.Normalize(NormalizationForm.FormKC)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        return Hash($"{normalizedDescription}|{purchaseMarker}");
    }

    private static string NormalizeLabel(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Trim().ToUpperInvariant();
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static InvalidDataException InvalidCell(string sheetName, int row, string field)
    {
        return new InvalidDataException(
            $"Workbook sheet '{sheetName}', row {row}, contains invalid {field} data.");
    }

    private sealed record MonthColumns(int Number, IReadOnlyDictionary<string, int> Columns);

    private sealed class DebtAccumulator(
        string sourceKey,
        string description,
        int installmentCount,
        int dueDay)
    {
        public string SourceKey { get; } = sourceKey;
        public string Description { get; } = description;
        public int InstallmentCount { get; } = installmentCount;
        public int DueDay { get; } = dueDay;
        public Dictionary<string, DebtWorkbookInstallment> Installments { get; } =
            new(StringComparer.Ordinal);
    }
}
