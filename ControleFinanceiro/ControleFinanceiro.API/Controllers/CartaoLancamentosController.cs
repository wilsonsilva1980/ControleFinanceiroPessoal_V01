using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CartaoLancamentosController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CartaoLancamento>>> GetLancamentos() =>
        await context.CartaoLancamentos.AsNoTracking().ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CartaoLancamento>> GetLancamento(int id)
    {
        var lancamento = await context.CartaoLancamentos.FindAsync(id);
        return lancamento is null ? NotFound() : lancamento;
    }

    [HttpGet("fatura")]
    public async Task<ActionResult<IEnumerable<CartaoLancamento>>> GetFaturaPorMes(
        [FromQuery] string mes,
        [FromQuery] int ano)
    {
        if (string.IsNullOrWhiteSpace(mes))
        {
            return BadRequest("Informe o mês da fatura.");
        }

        return await context.CartaoLancamentos
            .AsNoTracking()
            .Where(item => item.MesFatura.ToLower() == mes.ToLower() && item.AnoFatura == ano)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<CartaoLancamento>> PostLancamento(CartaoLancamento lancamento)
    {
        context.CartaoLancamentos.Add(lancamento);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLancamento), new { id = lancamento.Id }, lancamento);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLancamento(int id)
    {
        var lancamento = await context.CartaoLancamentos.FindAsync(id);
        if (lancamento is null)
        {
            return NotFound();
        }

        context.CartaoLancamentos.Remove(lancamento);
        await context.SaveChangesAsync();
        return NoContent();
    }
}