using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContasCasaController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContaCasa>>> GetContasCasa() =>
        await context.ContasCasa.AsNoTracking().ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContaCasa>> GetContaCasa(int id)
    {
        var conta = await context.ContasCasa.FindAsync(id);
        return conta is null ? NotFound() : conta;
    }

    [HttpPost]
    public async Task<ActionResult<ContaCasa>> PostContaCasa(ContaCasa contaCasa)
    {
        context.ContasCasa.Add(contaCasa);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetContaCasa), new { id = contaCasa.Id }, contaCasa);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutContaCasa(int id, ContaCasa contaCasa)
    {
        if (id != contaCasa.Id)
        {
            return BadRequest();
        }

        context.Entry(contaCasa).State = EntityState.Modified;
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.ContasCasa.AnyAsync(item => item.Id == id))
            {
                return NotFound();
            }

            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteContaCasa(int id)
    {
        var conta = await context.ContasCasa.FindAsync(id);
        if (conta is null)
        {
            return NotFound();
        }

        context.ContasCasa.Remove(conta);
        await context.SaveChangesAsync();
        return NoContent();
    }
}