using System.Xml;
using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.DebtImport;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ControleFinanceiro.API.Services;

public sealed class DebtWorkbookImportService(
    IConfiguration configuration,
    ILogger<DebtWorkbookImportService> logger) : IDebtWorkbookImportService
{
    private const long MaxWorkbookSizeBytes = 10 * 1024 * 1024;
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("The DefaultConnection connection string is missing.");

    public async Task<DebtWorkbookPreviewResponse> PreviewAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var tempPath = await SaveTemporaryFileAsync(file, cancellationToken);
        try
        {
            var plan = ParseWorkbook(tempPath);
            return new DebtWorkbookPreviewResponse(
                plan.Debts.Count,
                plan.Debts.Sum(debt => debt.Installments.Count),
                plan.UndatedEarlyPaymentsSkipped);
        }
        finally
        {
            DeleteTemporaryFile(tempPath);
        }
    }

    public async Task<DebtWorkbookImportResponse> ImportAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var tempPath = await SaveTemporaryFileAsync(file, cancellationToken);
        try
        {
            var plan = ParseWorkbook(tempPath);
            try
            {
                if (!await TrackingSchemaExistsAsync(cancellationToken))
                {
                    throw new InvalidOperationException("Apply migration '0009_debt_workbook_import_tracking.sql' before importing debt workbooks.");
                }

                var result = await new DebtWorkbookImporter(_connectionString).ImportAsync(tempPath, plan, cancellationToken);
                return new DebtWorkbookImportResponse(
                    result.BatchId,
                    result.DebtsInserted,
                    result.InstallmentsInserted,
                    result.AlreadyImported,
                    result.UndatedEarlyPaymentsSkipped);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError("Debt workbook persistence failed ({ExceptionType}).", ex.GetType().Name);
                throw new DebtWorkbookPersistenceException();
            }
        }
        finally
        {
            DeleteTemporaryFile(tempPath);
        }
    }

    private async Task<string> SaveTemporaryFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            throw new DebtWorkbookValidationException("A workbook file is required.");
        }

        if (file.Length <= 0)
        {
            throw new DebtWorkbookValidationException("The uploaded workbook is empty.");
        }

        if (file.Length > MaxWorkbookSizeBytes)
        {
            throw new DebtWorkbookValidationException("The uploaded workbook exceeds the 10 MiB limit.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new DebtWorkbookValidationException("Only .xlsx workbooks are supported.");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"debt-import-{Guid.NewGuid():N}.xlsx");
        var copyCompleted = false;
        try
        {
            await using (var output = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var input = file.OpenReadStream())
            {
                var buffer = new byte[81920];
                long copiedBytes = 0;
                int bytesRead;
                while ((bytesRead = await input.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
                {
                    copiedBytes += bytesRead;
                    if (copiedBytes > MaxWorkbookSizeBytes)
                    {
                        throw new DebtWorkbookValidationException("The uploaded workbook exceeds the 10 MiB limit.");
                    }

                    await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }

                if (copiedBytes == 0)
                {
                    throw new DebtWorkbookValidationException("The uploaded workbook is empty.");
                }
            }

            copyCompleted = true;
            return tempPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            logger.LogError("Debt workbook upload could not be stored temporarily ({ExceptionType}).", ex.GetType().Name);
            throw new DebtWorkbookProcessingException();
        }
        finally
        {
            if (!copyCompleted)
            {
                DeleteTemporaryFile(tempPath);
            }
        }
    }

    private static DebtWorkbookImportPlan ParseWorkbook(string tempPath)
    {
        try
        {
            return new DebtWorkbookParser().Parse(tempPath);
        }
        catch (InvalidDataException ex)
        {
            throw new DebtWorkbookValidationException(ex.Message);
        }
        catch (FileNotFoundException)
        {
            throw new DebtWorkbookValidationException("The uploaded workbook could not be read.");
        }
        catch (FormatException)
        {
            throw new DebtWorkbookValidationException("The uploaded file is not a valid .xlsx workbook.");
        }
        catch (InvalidOperationException)
        {
            throw new DebtWorkbookValidationException("The uploaded workbook could not be validated.");
        }
        catch (ArgumentException)
        {
            throw new DebtWorkbookValidationException("The uploaded file is not a valid .xlsx workbook.");
        }
        catch (XmlException)
        {
            throw new DebtWorkbookValidationException("The uploaded file is not a valid .xlsx workbook.");
        }
    }

    private async Task<bool> TrackingSchemaExistsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'debt_import_batches' AND TABLE_SCHEMA = 'dbo';";
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is int tableCount && tableCount > 0;
    }

    private void DeleteTemporaryFile(string tempPath)
    {
        try
        {
            File.Delete(tempPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Temporary debt workbook cleanup failed ({ExceptionType}).", ex.GetType().Name);
        }
    }
}
