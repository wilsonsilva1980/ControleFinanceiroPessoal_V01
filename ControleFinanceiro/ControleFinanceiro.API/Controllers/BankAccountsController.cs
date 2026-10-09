using ControleFinanceiro.API.Contracts;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class BankAccountsController(IFinancialAccountService accounts) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BankAccountResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BankAccountResponse>>> GetBankAccounts(CancellationToken cancellationToken) =>
        Ok(await accounts.GetBankAccountsAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BankAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankAccountResponse>> GetBankAccount(int id, CancellationToken cancellationToken)
    {
        var account = await accounts.GetBankAccountAsync(id, cancellationToken);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BankAccountResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<BankAccountResponse>> CreateBankAccount(
        BankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await accounts.CreateBankAccountAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBankAccount), new { id = account.Id }, account);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBankAccount(
        int id,
        BankAccountRequest request,
        CancellationToken cancellationToken) =>
        await accounts.UpdateBankAccountAsync(id, request, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBankAccount(int id, CancellationToken cancellationToken) =>
        await accounts.DeleteBankAccountAsync(id, cancellationToken) ? NoContent() : NotFound();
}