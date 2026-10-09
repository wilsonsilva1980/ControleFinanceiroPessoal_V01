using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class TripBudgetsController(ITripBudgetService tripBudgets) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TripBudgetResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TripBudgetResponse>>> GetTripBudgets(CancellationToken cancellationToken) =>
        Ok(await tripBudgets.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TripBudgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripBudgetResponse>> GetTripBudget(int id, CancellationToken cancellationToken)
    {
        var budget = await tripBudgets.GetByIdAsync(id, cancellationToken);
        return budget is null ? NotFound() : Ok(budget);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TripBudgetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TripBudgetResponse>> CreateTripBudget(
        CreateTripBudgetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var budget = await tripBudgets.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetTripBudget), new { id = budget.Id }, budget);
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
    public async Task<IActionResult> UpdateTripBudget(
        int id,
        UpdateTripBudgetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await tripBudgets.UpdateAsync(id, request, cancellationToken) ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTripBudget(int id, CancellationToken cancellationToken) =>
        await tripBudgets.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{budgetId:int}/items")]
    [ProducesResponseType(typeof(TripCostItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripCostItemResponse>> CreateTripCostItem(
        int budgetId,
        CreateTripCostItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await tripBudgets.CreateItemAsync(budgetId, request, cancellationToken);
            return item is null
                ? NotFound()
                : CreatedAtAction(nameof(GetTripBudget), new { id = budgetId }, item);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("items/{costItemId:int}/payments")]
    [ProducesResponseType(typeof(TripPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripPaymentResponse>> CreateTripPayment(
        int costItemId,
        CreateTripPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var payment = await tripBudgets.CreatePaymentAsync(costItemId, request, cancellationToken);
            return payment is null
                ? NotFound()
                : StatusCode(StatusCodes.Status201Created, payment);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
