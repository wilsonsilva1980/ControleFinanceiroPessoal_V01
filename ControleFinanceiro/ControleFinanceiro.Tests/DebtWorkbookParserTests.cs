using ClosedXML.Excel;
using ControleFinanceiro.DebtImport;
using Xunit;

namespace ControleFinanceiro.Tests;

public class DebtWorkbookParserTests
{
    [Fact]
    public void Parse_CombinesYearlyInstallmentsAndNormalizesLegacyPaidMarkers()
    {
        var path = CreateWorkbook(includeEarlyPayment: false);
        try
        {
            var plan = new DebtWorkbookParser().Parse(path);

            var debt = Assert.Single(plan.Debts);
            Assert.Equal("Fixture purchase", debt.Description);
            Assert.Equal(new DateTime(2024, 1, 5), debt.FirstDueDate);
            Assert.Equal(50.00m, debt.OriginalValue);
            Assert.Equal(3, debt.InstallmentCount);
            Assert.Equal(4, debt.Installments.Count);
            Assert.Equal("Pago", debt.Installments[0].Status);
            Assert.Null(debt.Installments[0].PaymentDate);
            Assert.Equal("Pendente", debt.Installments[1].Status);
            Assert.Equal(0, debt.Installments[3].Number);
            Assert.Equal(new DateTime(2026, 1, 5), debt.Installments[3].DueDate);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_SkipsUndatedEarlyPaymentsWithoutExposingSourceValues()
    {
        var path = CreateWorkbook(includeEarlyPayment: true);
        try
        {
            var plan = new DebtWorkbookParser().Parse(path);

            Assert.Equal(1, plan.UndatedEarlyPaymentsSkipped);
            Assert.Single(plan.Debts);
            Assert.Equal(4, plan.Debts[0].Installments.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_UsesBrazilianSeparatorsForTextAmounts()
    {
        var path = CreateWorkbook(includeEarlyPayment: false);
        try
        {
            using (var workbook = new XLWorkbook(path))
            {
                workbook.Worksheet("Controle Pagamentos - 2024").Cell(5, 6).Value = "R$ 1.234,56";
                workbook.Save();
            }

            var debt = Assert.Single(new DebtWorkbookParser().Parse(path).Debts);

            Assert.Equal(1272.06m, debt.OriginalValue);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_RoundsInstallmentAmountsToDatabasePrecision()
    {
        var path = CreateWorkbook(includeEarlyPayment: false);
        try
        {
            using (var workbook = new XLWorkbook(path))
            {
                workbook.Worksheet("Controle Pagamentos - 2024").Cell(5, 6).Value = 12.555m;
                workbook.Save();
            }

            var debt = Assert.Single(new DebtWorkbookParser().Parse(path).Debts);

            Assert.Equal(12.56m, debt.Installments[0].Amount);
            Assert.Equal(50.06m, debt.OriginalValue);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateWorkbook(bool includeEarlyPayment)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();

        AddSchedule(workbook, "Controle Pagamentos - 2024", [(1, 1), (2, 2)], "P");
        AddSchedule(workbook, "Controle Pagamentos - 2025", [(1, 3)], "");
        AddSchedule(workbook, "Controle Pagamentos - 2026", [(1, 0)], "");

        var sheet = workbook.Worksheets.Add("CP - 2026 - Antecipação de Parc");
        sheet.Cell(4, 1).Value = "Dívida";
        sheet.Cell(4, 2).Value = "Data Compra";
        sheet.Cell(4, 3).Value = "Qtde Parcelas";
        sheet.Cell(4, 4).Value = "Data - Venc.";
        var monthLabels = new[]
        {
            "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
            "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
        };
        var start = 5;
        for (var month = 1; month <= 12; month++)
        {
            sheet.Cell(3, start).Value = monthLabels[month];
            WriteInstallmentHeaders(sheet, start);
            if (month == 4)
            {
                sheet.Cell(4, start + 4).Value = "Valor Quitação";
                sheet.Cell(4, start + 5).Value = "Quitação Pago";
                sheet.Cell(4, start + 6).Value = "Pagamento";
                sheet.Cell(4, start + 7).Value = "Status";
                start += 8;
            }
            else
            {
                start += 6;
            }
        }

        if (includeEarlyPayment)
        {
            sheet.Cell(5, 1).Value = "Fixture purchase";
            sheet.Cell(5, 2).Value = new DateTime(2023, 12, 1);
            sheet.Cell(5, 3).Value = 3;
            sheet.Cell(5, 4).Value = 5;
            sheet.Cell(5, 27).Value = 50m;
            sheet.Cell(5, 28).Value = 50m;
            sheet.Cell(6, 1).Value = "Fixture projected payoff";
            sheet.Cell(6, 27).Value = 25m;
        }

        workbook.SaveAs(path);
        return path;
    }

    private static void AddSchedule(
        XLWorkbook workbook,
        string name,
        (int Month, int Number)[] installments,
        string status)
    {
        var sheet = workbook.Worksheets.Add(name);
        sheet.Cell(4, 1).Value = "Dívida";
        sheet.Cell(4, 2).Value = "Data Compra";
        sheet.Cell(4, 3).Value = "Qtde Parcelas";
        sheet.Cell(4, 4).Value = "Data - Venc.";

        for (var month = 1; month <= 12; month++)
        {
            var start = 5 + (month - 1) * 6;
            sheet.Cell(3, start).Value = new[]
            {
                "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
                "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
            }[month];
            WriteInstallmentHeaders(sheet, start);
        }

        sheet.Cell(5, 1).Value = "Fixture purchase";
        sheet.Cell(5, 2).Value = new DateTime(2023, 12, 1);
        sheet.Cell(5, 3).Value = 3;
        sheet.Cell(5, 4).Value = 5;
        foreach (var (month, installmentNumber) in installments)
        {
            var start = 5 + (month - 1) * 6;
            sheet.Cell(5, start).Value = installmentNumber;
            sheet.Cell(5, start + 1).Value = 12.50m;
            sheet.Cell(5, start + 3).Value = installmentNumber == 1 && status == "P" ? 12.50m : 0m;
            sheet.Cell(5, start + 5).Value = installmentNumber == 1 ? status : "";
        }
    }

    private static void WriteInstallmentHeaders(IXLWorksheet sheet, int start)
    {
        sheet.Cell(4, start).Value = "Parcela";
        sheet.Cell(4, start + 1).Value = "Valor";
        sheet.Cell(4, start + 2).Value = "Valor Faltante";
        sheet.Cell(4, start + 3).Value = "Valor Pago";
        sheet.Cell(4, start + 4).Value = "Pagamento";
        sheet.Cell(4, start + 5).Value = "Status";
    }
}
