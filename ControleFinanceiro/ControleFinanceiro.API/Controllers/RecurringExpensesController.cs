using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class RecurringExpensesController(IRecurringExpenseService recurringExpenses) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringExpenseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecurringExpenseResponse>>> GetRecurringExpenses(CancellationToken cancellationToken) =>
        Ok(await recurringExpenses.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RecurringExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringExpenseResponse>> GetRecurringExpense(int id, CancellationToken cancellationToken)
    {
        var expense = await recurringExpenses.GetByIdAsync(id, cancellationToken);
        return expense is null ? NotFound() : Ok(expense);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RecurringExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurringExpenseResponse>> CreateRecurringExpense(
        RecurringExpenseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var expense = await recurringExpenses.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetRecurringExpense), new { id = expense.Id }, expense);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRecurringExpense(
        int id,
        RecurringExpenseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await recurringExpenses.UpdateAsync(id, request, cancellationToken) ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRecurringExpense(int id, CancellationToken cancellationToken) =>
        await recurringExpenses.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("projection/monthly")]
    [ProducesResponseType(typeof(RecurringExpenseProjectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurringExpenseProjectionResponse>> GetMonthlyProjection(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (year is < 1 or > 9998 || month is < 1 or > 12)
        {
            return BadRequest("Provide a valid year and month.");
        }

        return Ok(await recurringExpenses.GetMonthlyProjectionAsync(year, month, cancellationToken));
    }

    [HttpPost("occurrences/{id:int}/link")]
    [ProducesResponseType(typeof(RecurringExpenseOccurrenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RecurringExpenseOccurrenceResponse>> LinkOccurrence(
        int id,
        LinkRecurringExpenseOccurrenceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var occurrence = await recurringExpenses.LinkOccurrenceAsync(id, request, cancellationToken);
            return occurrence is null ? NotFound() : Ok(occurrence);
        }
        catch (RecurringExpenseLinkException exception)
        {
            return exception.Failure switch
            {
                RecurringExpenseLinkFailure.InvalidCombination => BadRequest(exception.Message),
                RecurringExpenseLinkFailure.LedgerRecordNotFound => NotFound(),
                RecurringExpenseLinkFailure.AlreadyLinked => Conflict(exception.Message),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
    }
}