using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControleFinanceiro.Tests;

public class ContasCasaControllerTests
{
    [Fact]
    public async Task GetContasCasa_RetornaContasCadastradas()
    {
        using var context = TestDbContextFactory.Create();
        context.ContasCasa.Add(new ContaCasa { Descricao = "Aluguel" });
        await context.SaveChangesAsync();
        var controller = new ContasCasaController(context);

        var result = await controller.GetContasCasa();

        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task GetContaCasa_IdInexistente_RetornaNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ContasCasaController(context);

        var result = await controller.GetContaCasa(42);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task PostContaCasa_CriaConta()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ContasCasaController(context);

        var result = await controller.PostContaCasa(new ContaCasa { Descricao = "Aluguel" });

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(1, await context.ContasCasa.CountAsync());
    }

    [Fact]
    public async Task PutContaCasa_AtualizaContaExistente()
    {
        using var context = TestDbContextFactory.Create();
        var conta = new ContaCasa { Descricao = "Aluguel" };
        context.ContasCasa.Add(conta);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var controller = new ContasCasaController(context);

        var result = await controller.PutContaCasa(conta.Id, new ContaCasa { Id = conta.Id, Descricao = "Condomínio" });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Condomínio", (await context.ContasCasa.FindAsync(conta.Id))!.Descricao);
    }

    [Fact]
    public async Task DeleteContaCasa_IdInexistente_RetornaNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ContasCasaController(context);

        var result = await controller.DeleteContaCasa(42);

        Assert.IsType<NotFoundResult>(result);
    }
}