using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EntradasController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Entrada>>> GetEntradas() =>
        await context.Entradas.AsNoTracking().ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Entrada>> GetEntrada(int id)
    {
        var entrada = await context.Entradas.FindAsync(id);
        return entrada is null ? NotFound() : entrada;
    }

    [HttpPost]
    public async Task<ActionResult<Entrada>> PostEntrada(Entrada entrada)
    {
        context.Entradas.Add(entrada);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEntrada), new { id = entrada.Id }, entrada);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutEntrada(int id, Entrada entrada)
    {
        if (id != entrada.Id)
        {
            return BadRequest();
        }

        context.Entry(entrada).State = EntityState.Modified;
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.Entradas.AnyAsync(item => item.Id == id))
            {
                return NotFound();
            }

            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEntrada(int id)
    {
        var entrada = await context.Entradas.FindAsync(id);
        if (entrada is null)
        {
            return NotFound();
        }

        context.Entradas.Remove(entrada);
        await context.SaveChangesAsync();
        return NoContent();
    }
}