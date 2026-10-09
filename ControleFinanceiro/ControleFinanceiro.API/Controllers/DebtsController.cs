using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class DebtsController(IDebtService debtService, IDebtWorkbookImportService debtWorkbookImportService) : ControllerBase
{
    private const long MaxWorkbookRequestSizeBytes = 10 * 1024 * 1024;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DebtResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DebtResponse>>> GetDebts(CancellationToken cancellationToken) =>
        Ok(await debtService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DebtResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtResponse>> GetDebt(int id, CancellationToken cancellationToken)
    {
        var debt = await debtService.GetByIdAsync(id, cancellationToken);
        return debt is null ? NotFound() : Ok(debt);
    }

    [HttpGet("summary/monthly")]
    [ProducesResponseType(typeof(DebtPeriodSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DebtPeriodSummaryResponse>> GetMonthlySummary(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (year is < 1 or > 9998 || month is < 1 or > 12)
        {
            return BadRequest("Provide a valid year and month.");
        }

        return Ok(await debtService.GetPeriodSummaryAsync(year, month, cancellationToken));
    }

    [HttpGet("summary/yearly")]
    [ProducesResponseType(typeof(DebtPeriodSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DebtPeriodSummaryResponse>> GetYearlySummary(
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        if (year is < 1 or > 9998)
        {
            return BadRequest("Provide a valid year.");
        }

        return Ok(await debtService.GetPeriodSummaryAsync(year, null, cancellationToken));
    }

    [HttpPost("upload-preview")]
    [HttpPost("preview")]
    [HttpPost("import/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxWorkbookRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxWorkbookRequestSizeBytes)]
    [ProducesResponseType(typeof(DebtWorkbookPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DebtWorkbookPreviewResponse>> PreviewWorkbook(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await debtWorkbookImportService.PreviewAsync(file, cancellationToken);
            return Ok(result);
        }
        catch (DebtWorkbookValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DebtWorkbookProcessingException)
        {
            return Problem(
                title: "Debt workbook preview failed.",
                detail: "The uploaded workbook could not be processed.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("upload")]
    [HttpPost("confirm")]
    [HttpPost("import")]
    [HttpPost("import/confirm")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxWorkbookRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxWorkbookRequestSizeBytes)]
    [ProducesResponseType(typeof(DebtWorkbookImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DebtWorkbookImportResponse>> ImportWorkbook(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await debtWorkbookImportService.ImportAsync(file, cancellationToken);
            return Ok(result);
        }
        catch (DebtWorkbookValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DebtWorkbookProcessingException)
        {
            return Problem(
                title: "Debt workbook import failed.",
                detail: "The uploaded workbook could not be processed.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (DebtWorkbookPersistenceException)
        {
            return Problem(
                title: "Debt workbook import failed.",
                detail: "The import could not be completed.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(DebtResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DebtResponse>> CreateDebt(
        CreateDebtRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var debt = await debtService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetDebt), new { id = debt.Id }, debt);
        }
        catch (DebtOutflowLinkException exception)
        {
            return ToActionResult(exception);
        }
    }

    [HttpPut("installments/{id:int}/payment")]
    [ProducesResponseType(typeof(DebtInstallmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtInstallmentResponse>> UpdateInstallmentPayment(
        int id,
        UpdateDebtInstallmentPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status is not DebtInstallment.PendingStatus and not DebtInstallment.PaidStatus)
        {
            return BadRequest("Status must be Pendente or Pago.");
        }

        if (request.Status == DebtInstallment.PaidStatus && request.PaymentDate is null)
        {
            return BadRequest("A payment date is required when an installment is Pago.");
        }

        var installment = await debtService.UpdateInstallmentPaymentAsync(id, request, cancellationToken);
        return installment is null ? NotFound() : Ok(installment);
    }

    [HttpPost("{id:int}/early-payments")]
    [ProducesResponseType(typeof(DebtEarlyPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtEarlyPaymentResponse>> CreateEarlyPayment(
        int id,
        CreateDebtEarlyPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("The early payment amount must be greater than zero.");
        }

        try
        {
            var payment = await debtService.CreateEarlyPaymentAsync(id, request, cancellationToken);
            return payment is null
                ? NotFound()
                : CreatedAtAction(nameof(GetDebt), new { id }, payment);
        }
        catch (DebtOutflowLinkException exception)
        {
            return ToActionResult(exception);
        }
    }

    private ActionResult ToActionResult(DebtOutflowLinkException exception) => exception.Failure switch
    {
        DebtOutflowLinkFailure.InvalidCombination => BadRequest(exception.Message),
        DebtOutflowLinkFailure.LedgerRecordNotFound => NotFound(),
        DebtOutflowLinkFailure.AlreadyLinked => Conflict(exception.Message),
        _ => throw exception
    };
}