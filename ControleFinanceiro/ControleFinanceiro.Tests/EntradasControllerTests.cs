using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControleFinanceiro.Tests;

public class EntradasControllerTests
{
    [Fact]
    public async Task GetEntradas_RetornaEntradasCadastradas()
    {
        using var context = TestDbContextFactory.Create();
        context.Entradas.Add(new Entrada { Descricao = "Salário" });
        await context.SaveChangesAsync();
        var controller = new EntradasController(context);

        var result = await controller.GetEntradas();

        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task GetEntrada_IdInexistente_RetornaNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new EntradasController(context);

        var result = await controller.GetEntrada(42);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task PostEntrada_CriaEntrada()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new EntradasController(context);

        var result = await controller.PostEntrada(new Entrada { Descricao = "Salário" });

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(1, await context.Entradas.CountAsync());
    }

    [Fact]
    public async Task PutEntrada_IdDiferenteDoModelo_RetornaBadRequest()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new EntradasController(context);

        var result = await controller.PutEntrada(1, new Entrada { Id = 2 });

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task PutEntrada_AtualizaEntradaExistente()
    {
        using var context = TestDbContextFactory.Create();
        var entrada = new Entrada { Descricao = "Salário" };
        context.Entradas.Add(entrada);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var controller = new EntradasController(context);

        var result = await controller.PutEntrada(entrada.Id, new Entrada { Id = entrada.Id, Descricao = "Novo salário" });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Novo salário", (await context.Entradas.FindAsync(entrada.Id))!.Descricao);
    }

    [Fact]
    public async Task DeleteEntrada_RemoveEntradaExistente()
    {
        using var context = TestDbContextFactory.Create();
        var entrada = new Entrada { Descricao = "Salário" };
        context.Entradas.Add(entrada);
        await context.SaveChangesAsync();
        var controller = new EntradasController(context);

        var result = await controller.DeleteEntrada(entrada.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await context.Entradas.FindAsync(entrada.Id));
    }
}