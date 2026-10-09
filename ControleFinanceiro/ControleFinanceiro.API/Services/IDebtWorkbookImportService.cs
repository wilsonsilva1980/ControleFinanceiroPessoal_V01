using ControleFinanceiro.API.Contracts;
using Microsoft.AspNetCore.Http;

namespace ControleFinanceiro.API.Services;

public interface IDebtWorkbookImportService
{
    Task<DebtWorkbookPreviewResponse> PreviewAsync(IFormFile file, CancellationToken cancellationToken);
    Task<DebtWorkbookImportResponse> ImportAsync(IFormFile file, CancellationToken cancellationToken);
}

public sealed class DebtWorkbookValidationException(string message) : Exception(message);

public sealed class DebtWorkbookProcessingException : Exception;

public sealed class DebtWorkbookPersistenceException : Exception;
