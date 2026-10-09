using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using ControleFinanceiro.DebtImport;

var path = Path.Combine(Path.GetTempPath(), $"debt-preview-{Guid.NewGuid():N}.xlsx");
using (var workbook = new XLWorkbook())
{
    AddSchedule(workbook, "Controle Pagamentos - 2024", [(1, 1), (2, 2)], "P");
    AddSchedule(workbook, "Controle Pagamentos - 2025", [(1, 3)], "");
    AddSchedule(workbook, "Controle Pagamentos - 2026", [(1, 0)], "");

    var earlyPaymentSheet = workbook.Worksheets.Add("CP - 2026 - Antecipação de Parc");
    earlyPaymentSheet.Cell(4, 1).Value = "Dívida";
    earlyPaymentSheet.Cell(4, 2).Value = "Data Compra";
    earlyPaymentSheet.Cell(4, 3).Value = "Qtde Parcelas";
    earlyPaymentSheet.Cell(4, 4).Value = "Data - Venc.";
    var monthLabels = new[] { "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro" };
    var start = 5;
    for (var month = 1; month <= 12; month++)
    {
        earlyPaymentSheet.Cell(3, start).Value = monthLabels[month];
        WriteInstallmentHeaders(earlyPaymentSheet, start);
        if (month == 4)
        {
            earlyPaymentSheet.Cell(4, start + 4).Value = "Valor Quitação";
            earlyPaymentSheet.Cell(4, start + 5).Value = "Quitação Pago";
            earlyPaymentSheet.Cell(4, start + 6).Value = "Pagamento";
            earlyPaymentSheet.Cell(4, start + 7).Value = "Status";
            start += 8;
        }
        else
        {
            start += 6;
        }
    }
    workbook.SaveAs(path);
}

Console.WriteLine(path);
try
{
    var plan = new DebtWorkbookParser().Parse(path);
    Console.WriteLine($"Debts:{plan.Debts.Count} Installments:{plan.Debts.Sum(d => d.Installments.Count)} early:{plan.UndatedEarlyPaymentsSkipped}");
    foreach (var debt in plan.Debts)
    {
        Console.WriteLine($"Debt {debt.Description}: {debt.Installments.Count}");
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
    Console.WriteLine(ex.StackTrace);
}

static void AddSchedule(XLWorkbook workbook, string name, (int Month, int Number)[] installments, string status)
{
    var sheet = workbook.Worksheets.Add(name);
    sheet.Cell(4, 1).Value = "Dívida";
    sheet.Cell(4, 2).Value = "Data Compra";
    sheet.Cell(4, 3).Value = "Qtde Parcelas";
    sheet.Cell(4, 4).Value = "Data - Venc.";

    for (var month = 1; month <= 12; month++)
    {
        var start = 5 + (month - 1) * 6;
        sheet.Cell(3, start).Value = new[] { "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro" }[month];
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

static void WriteInstallmentHeaders(IXLWorksheet sheet, int start)
{
    sheet.Cell(4, start).Value = "Parcela";
    sheet.Cell(4, start + 1).Value = "Valor";
    sheet.Cell(4, start + 2).Value = "Valor Faltante";
    sheet.Cell(4, start + 3).Value = "Valor Pago";
    sheet.Cell(4, start + 4).Value = "Pagamento";
    sheet.Cell(4, start + 5).Value = "Status";
}
