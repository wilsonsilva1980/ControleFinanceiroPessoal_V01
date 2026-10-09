using System.Text;
using ClosedXML.Excel;
using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ControleFinanceiro.Tests;

public class DebtsControllerTests
{
    [Fact]
    public async Task CreateDebt_PersistsDebtAndInstallments()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.CreateDebt(CreateDebtRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(1, await context.Debts.CountAsync());
        Assert.Equal(2, await context.DebtInstallments.CountAsync());
        Assert.Equal(nameof(DebtsController.GetDebt), created.ActionName);
    }

    [Fact]
    public async Task CreateDebt_LinksExistingHouseholdBillAndCardPurchase()
    {
        using var context = TestDbContextFactory.Create();
        var bill = new ContaCasa { Descricao = "Utility", DataVencimento = new DateTime(2026, 10, 10) };
        var cardPurchase = new CartaoLancamento { Estabelecimento = "Store", MesFatura = "novembro", AnoFatura = 2026 };
        context.AddRange(bill, cardPurchase);
        await context.SaveChangesAsync();
        var request = CreateDebtRequest() with
        {
            Installments =
            [
                new CreateDebtInstallmentRequest
                {
                    DueDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero),
                    ScheduledAmount = 100m,
                    LinkedHouseBillId = bill.Id
                },
                new CreateDebtInstallmentRequest
                {
                    DueDate = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero),
                    ScheduledAmount = 100m,
                    LinkedCardPurchaseId = cardPurchase.Id
                }
            ]
        };

        var result = await CreateController(context).CreateDebt(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<DebtResponse>(created.Value);
        Assert.Equal(bill.Id, response.Installments[0].LinkedHouseBillId);
        Assert.Equal(cardPurchase.Id, response.Installments[1].LinkedCardPurchaseId);
        Assert.Equal(2, await context.DebtOutflowLinks.CountAsync());
    }

    [Fact]
    public async Task CreateDebt_RejectsLinkingOneExpenseMoreThanOnce()
    {
        using var context = TestDbContextFactory.Create();
        var bill = new ContaCasa { Descricao = "Utility", DataVencimento = new DateTime(2026, 10, 10) };
        context.ContasCasa.Add(bill);
        await context.SaveChangesAsync();
        var request = CreateDebtRequest() with
        {
            Installments =
            [
                new CreateDebtInstallmentRequest
                {
                    DueDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero),
                    ScheduledAmount = 100m,
                    LinkedHouseBillId = bill.Id
                },
                new CreateDebtInstallmentRequest
                {
                    DueDate = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero),
                    ScheduledAmount = 100m,
                    LinkedHouseBillId = bill.Id
                }
            ]
        };

        var result = await CreateController(context).CreateDebt(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Empty(await context.Debts.ToListAsync());
    }

    [Fact]
    public async Task UpdateInstallmentPayment_MarksInstallmentPaidAndDerivesAmounts()
    {
        using var context = TestDbContextFactory.Create();
        var debt = new Debt { Description = "Debt", DueDate = new DateTime(2026, 10, 10), OriginalAmount = 100m };
        debt.Installments.Add(new DebtInstallment
        {
            DueDate = new DateTime(2026, 10, 10),
            ScheduledAmount = 100m
        });
        context.Debts.Add(debt);
        await context.SaveChangesAsync();
        var installment = await context.DebtInstallments.SingleAsync();
        var controller = CreateController(context);

        var result = await controller.UpdateInstallmentPayment(
            installment.Id,
            new UpdateDebtInstallmentPaymentRequest
            {
                Status = DebtInstallment.PaidStatus,
                PaymentDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero)
            },
            CancellationToken.None);

        var updated = Assert.IsType<OkObjectResult>(result.Result).Value as DebtInstallmentResponse;
        Assert.NotNull(updated);
        Assert.Equal(100m, updated.PaidAmount);
        Assert.Equal(0m, updated.RemainingAmount);
    }

    [Fact]
    public async Task UpdateInstallmentPayment_RejectsInvalidStatusAndMissingPaymentDate()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var invalidStatus = await controller.UpdateInstallmentPayment(
            1,
            new UpdateDebtInstallmentPaymentRequest { Status = "Other" },
            CancellationToken.None);
        var missingDate = await controller.UpdateInstallmentPayment(
            1,
            new UpdateDebtInstallmentPaymentRequest { Status = DebtInstallment.PaidStatus },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(invalidStatus.Result);
        Assert.IsType<BadRequestObjectResult>(missingDate.Result);
    }

    [Fact]
    public async Task UpdateInstallmentPayment_ReturnsNotFoundForMissingInstallment()
    {
        using var context = TestDbContextFactory.Create();
        var result = await CreateController(context).UpdateInstallmentPayment(
            42,
            new UpdateDebtInstallmentPaymentRequest { Status = DebtInstallment.PendingStatus },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateEarlyPayment_PersistsPaymentAndReturnsNotFoundForMissingDebt()
    {
        using var context = TestDbContextFactory.Create();
        var debt = new Debt { Description = "Debt", DueDate = new DateTime(2026, 10, 10), OriginalAmount = 100m };
        context.Debts.Add(debt);
        await context.SaveChangesAsync();
        var controller = CreateController(context);
        var request = new CreateDebtEarlyPaymentRequest
        {
            PaymentDate = new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero),
            Amount = 25m
        };

        var created = await controller.CreateEarlyPayment(debt.Id, request, CancellationToken.None);
        var missing = await controller.CreateEarlyPayment(42, request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(created.Result);
        Assert.Equal(1, await context.DebtEarlyPayments.CountAsync());
        Assert.IsType<NotFoundResult>(missing.Result);
    }

    [Fact]
    public async Task PreviewWorkbook_RejectsNonXlsxFiles()
    {
        var controller = CreateController(TestDbContextFactory.Create());
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an xlsx"));
        var file = new FormFile(stream, 0, stream.Length, "file", "test.txt");

        var result = await controller.PreviewWorkbook(file, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Only .xlsx workbooks", badRequest.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task PreviewWorkbook_RejectsFilesLargerThanTenMiB()
    {
        var controller = CreateController(TestDbContextFactory.Create());
        using var stream = new MemoryStream(new byte[10 * 1024 * 1024 + 1]);
        var file = new FormFile(stream, 0, stream.Length, "file", "oversized.xlsx");

        var result = await controller.PreviewWorkbook(file, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("10 MiB", badRequest.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task PreviewWorkbook_RejectsInvalidWorkbookWithoutWritingDebts()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var temporaryFilesBefore = GetDebtImportTemporaryFiles();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("invalid workbook"));
        var file = new FormFile(stream, 0, stream.Length, "file", "invalid.xlsx");

        try
        {
            var result = await controller.PreviewWorkbook(file, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.DoesNotContain("invalid workbook", System.Text.Json.JsonSerializer.Serialize(badRequest.Value));
            Assert.Empty(await context.Debts.ToListAsync());
            Assert.Empty(await context.DebtInstallments.ToListAsync());
            Assert.Equal(temporaryFilesBefore, GetDebtImportTemporaryFiles());
        }
        finally
        {
            foreach (var temporaryFile in GetDebtImportTemporaryFiles().Except(temporaryFilesBefore))
            {
                File.Delete(temporaryFile);
            }
        }
    }

    [Fact]
    public async Task PreviewWorkbook_RejectsOversizedStreamEvenWhenReportedLengthIsSmall()
    {
        var controller = CreateController(TestDbContextFactory.Create());
        var content = new byte[10 * 1024 * 1024 + 1];
        var file = new TestWorkbookFile("oversized.xlsx", 1, () => new MemoryStream(content));

        var result = await controller.PreviewWorkbook(file, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("10 MiB", badRequest.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task PreviewWorkbook_RemovesTemporaryFileWhenUploadStreamFails()
    {
        var temporaryFilesBefore = GetDebtImportTemporaryFiles();
        var file = new TestWorkbookFile("broken.xlsx", 1, () => throw new IOException("private temporary path"));

        try
        {
            await Assert.ThrowsAsync<DebtWorkbookProcessingException>(() =>
                CreateImportService().PreviewAsync(file, CancellationToken.None));

            Assert.Equal(temporaryFilesBefore, GetDebtImportTemporaryFiles());
        }
        finally
        {
            foreach (var temporaryFile in GetDebtImportTemporaryFiles().Except(temporaryFilesBefore))
            {
                File.Delete(temporaryFile);
            }
        }
    }

    [Fact]
    public async Task ImportWorkbook_ReturnsSafeFailureAndCleansTemporaryFileWhenDatabaseIsUnavailable()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new DebtsController(
            new DebtService(context, new RecurringExpenseService(context)),
            CreateImportService("Server=127.0.0.1,1;Database=ControleFinanceiro;Integrated Security=True;Connect Timeout=1;Encrypt=False"));
        var temporaryFilesBefore = GetDebtImportTemporaryFiles();
        var path = CreateSupportedWorkbook();
        try
        {
            await using var stream = File.OpenRead(path);
            var file = new FormFile(stream, 0, stream.Length, "file", "private-fixture.xlsx");

            var result = await controller.ImportWorkbook(file, CancellationToken.None);

            var failure = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, failure.StatusCode);
            var problem = Assert.IsType<ProblemDetails>(failure.Value);
            Assert.DoesNotContain("Fixture purchase", problem.Detail);
            Assert.DoesNotContain("private-fixture.xlsx", problem.Detail);
            Assert.Equal(temporaryFilesBefore, GetDebtImportTemporaryFiles());
            Assert.Empty(await context.Debts.ToListAsync());
        }
        finally
        {
            foreach (var temporaryFile in GetDebtImportTemporaryFiles().Except(temporaryFilesBefore))
            {
                File.Delete(temporaryFile);
            }

            File.Delete(path);
        }
    }

    [Fact]
    public void WorkbookUploadEndpoints_EnforceTenMiBRequestAndMultipartLimits()
    {
        var preview = typeof(DebtsController).GetMethod(nameof(DebtsController.PreviewWorkbook));
        var import = typeof(DebtsController).GetMethod(nameof(DebtsController.ImportWorkbook));

        AssertUploadLimits(preview);
        AssertUploadLimits(import);

        static void AssertUploadLimits(System.Reflection.MethodInfo? action)
        {
            Assert.NotNull(action);
            var requestLimit = action.CustomAttributes.Single(attribute =>
                attribute.AttributeType == typeof(RequestSizeLimitAttribute));
            var formLimit = action.CustomAttributes.Single(attribute =>
                attribute.AttributeType == typeof(RequestFormLimitsAttribute));

            Assert.Equal(10L * 1024 * 1024, Convert.ToInt64(requestLimit.ConstructorArguments.Single().Value));
            Assert.Equal(
                10L * 1024 * 1024,
                Convert.ToInt64(formLimit.NamedArguments.Single(argument =>
                    argument.MemberName == nameof(RequestFormLimitsAttribute.MultipartBodyLengthLimit)).TypedValue.Value));
        }
    }

    [Fact]
    public async Task PreviewWorkbook_ReturnsAggregateCountsForSupportedWorkbook()
    {
        var controller = CreateController(TestDbContextFactory.Create());
        var temporaryFilesBefore = GetDebtImportTemporaryFiles();
        var path = CreateSupportedWorkbook();
        try
        {
            await using var fileStream = File.OpenRead(path);
            var file = new FormFile(fileStream, 0, fileStream.Length, "file", "preview.xlsx");

            var result = await controller.PreviewWorkbook(file, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<DebtWorkbookPreviewResponse>(ok.Value);
            Assert.Equal(1, response.DebtCount);
            Assert.Equal(4, response.InstallmentCount);
            Assert.Equal(0, response.UndatedEarlyPaymentsSkipped);
            Assert.DoesNotContain("Fixture purchase", System.Text.Json.JsonSerializer.Serialize(response));
            Assert.Equal(temporaryFilesBefore, GetDebtImportTemporaryFiles());
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateSupportedWorkbook()
    {
        var path = Path.Combine(Path.GetTempPath(), $"debt-preview-{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();

        AddSchedule(workbook, "Controle Pagamentos - 2024", [(1, 1), (2, 2)], "P");
        AddSchedule(workbook, "Controle Pagamentos - 2025", [(1, 3)], "");
        AddSchedule(workbook, "Controle Pagamentos - 2026", [(1, 0)], "");

        var earlyPaymentSheet = workbook.Worksheets.Add("CP - 2026 - Antecipação de Parc");
        earlyPaymentSheet.Cell(4, 1).Value = "Dívida";
        earlyPaymentSheet.Cell(4, 2).Value = "Data Compra";
        earlyPaymentSheet.Cell(4, 3).Value = "Qtde Parcelas";
        earlyPaymentSheet.Cell(4, 4).Value = "Data - Venc.";
        var monthLabels = new[]
        {
            "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
            "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
        };
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
        return path;
    }

    private static void AddSchedule(XLWorkbook workbook, string name, (int Month, int Number)[] installments, string status)
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

    private static DebtsController CreateController(AppDbContext context) =>
        new(
            new DebtService(context, new RecurringExpenseService(context)),
            CreateImportService());

    private static DebtWorkbookImportService CreateImportService(
        string connectionString = "Server=(localdb)\\mssqllocaldb;Database=ControleFinanceiro;Trusted_Connection=True;") =>
        new(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString
                })
                .Build(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DebtWorkbookImportService>.Instance);

    private static string[] GetDebtImportTemporaryFiles() =>
        Directory.GetFiles(Path.GetTempPath(), "debt-import-*.xlsx").Order(StringComparer.Ordinal).ToArray();

    private sealed class TestWorkbookFile(string fileName, long length, Func<Stream> openReadStream) : IFormFile
    {
        public string ContentDisposition => string.Empty;
        public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public string FileName { get; } = fileName;
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public long Length { get; } = length;
        public string Name => "file";
        public Stream OpenReadStream() => openReadStream();
        public void CopyTo(Stream target) => OpenReadStream().CopyTo(target);
        public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            await using var source = OpenReadStream();
            await source.CopyToAsync(target, cancellationToken);
        }
    }

    private static CreateDebtRequest CreateDebtRequest() => new()
    {
        Description = "Example debt",
        DueDate = new DateTimeOffset(2026, 12, 10, 0, 0, 0, TimeSpan.Zero),
        OriginalAmount = 200m,
        Installments =
        [
            new CreateDebtInstallmentRequest
            {
                DueDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero),
                ScheduledAmount = 100m
            },
            new CreateDebtInstallmentRequest
            {
                DueDate = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero),
                ScheduledAmount = 100m
            }
        ]
    };
}