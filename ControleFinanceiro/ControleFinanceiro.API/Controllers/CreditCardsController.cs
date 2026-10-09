using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class CreditCardsController(IFinancialAccountService accounts) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CreditCardResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreditCardResponse>>> GetCreditCards(CancellationToken cancellationToken) =>
        Ok(await accounts.GetCreditCardsAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CreditCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardResponse>> GetCreditCard(int id, CancellationToken cancellationToken)
    {
        var card = await accounts.GetCreditCardAsync(id, cancellationToken);
        return card is null ? NotFound() : Ok(card);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreditCardResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreditCardResponse>> CreateCreditCard(
        CreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var card = await accounts.CreateCreditCardAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCreditCard), new { id = card.Id }, card);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCreditCard(
        int id,
        CreditCardRequest request,
        CancellationToken cancellationToken) =>
        await accounts.UpdateCreditCardAsync(id, request, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCreditCard(int id, CancellationToken cancellationToken) =>
        await accounts.DeleteCreditCardAsync(id, cancellationToken) ? NoContent() : NotFound();
}