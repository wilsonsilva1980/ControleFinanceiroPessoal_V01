using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControleFinanceiro.Tests;

public class DespesasVariaveisControllerTests
{
    [Fact]
    public async Task GetDespesasVariaveis_RetornaDespesasCadastradas()
    {
        using var context = TestDbContextFactory.Create();
        context.DespesasVariaveis.Add(new DespesaVariavel { Descricao = "Transporte" });
        await context.SaveChangesAsync();
        var controller = new DespesasVariaveisController(context);

        var result = await controller.GetDespesasVariaveis();

        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task GetDespesaVariavel_IdInexistente_RetornaNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new DespesasVariaveisController(context);

        var result = await controller.GetDespesaVariavel(42);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task PostDespesaVariavel_CriaDespesa()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new DespesasVariaveisController(context);

        var result = await controller.PostDespesaVariavel(new DespesaVariavel { Descricao = "Transporte" });

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(1, await context.DespesasVariaveis.CountAsync());
    }

    [Fact]
    public async Task DeleteDespesaVariavel_RemoveDespesaExistente()
    {
        using var context = TestDbContextFactory.Create();
        var despesa = new DespesaVariavel { Descricao = "Transporte" };
        context.DespesasVariaveis.Add(despesa);
        await context.SaveChangesAsync();
        var controller = new DespesasVariaveisController(context);

        var result = await controller.DeleteDespesaVariavel(despesa.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await context.DespesasVariaveis.FindAsync(despesa.Id));
    }
}