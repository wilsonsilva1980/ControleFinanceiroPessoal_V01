using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DespesasVariaveisController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DespesaVariavel>>> GetDespesasVariaveis() =>
        await context.DespesasVariaveis.AsNoTracking().ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DespesaVariavel>> GetDespesaVariavel(int id)
    {
        var despesa = await context.DespesasVariaveis.FindAsync(id);
        return despesa is null ? NotFound() : despesa;
    }

    [HttpPost]
    public async Task<ActionResult<DespesaVariavel>> PostDespesaVariavel(DespesaVariavel despesa)
    {
        context.DespesasVariaveis.Add(despesa);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetDespesaVariavel), new { id = despesa.Id }, despesa);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDespesaVariavel(int id)
    {
        var despesa = await context.DespesasVariaveis.FindAsync(id);
        if (despesa is null)
        {
            return NotFound();
        }

        context.DespesasVariaveis.Remove(despesa);
        await context.SaveChangesAsync();
        return NoContent();
    }
}