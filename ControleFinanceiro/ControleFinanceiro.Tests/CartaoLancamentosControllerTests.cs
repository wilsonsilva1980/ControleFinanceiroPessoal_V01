using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControleFinanceiro.Tests;

public class CartaoLancamentosControllerTests
{
    [Fact]
    public async Task GetFaturaPorMes_FiltraMesSemDiferenciarMaiusculasEAno()
    {
        using var context = TestDbContextFactory.Create();
        context.CartaoLancamentos.AddRange(
            new CartaoLancamento { Estabelecimento = "Mercado", MesFatura = "Outubro", AnoFatura = 2026 },
            new CartaoLancamento { Estabelecimento = "Farmácia", MesFatura = "Outubro", AnoFatura = 2025 });
        await context.SaveChangesAsync();
        var controller = new CartaoLancamentosController(context);

        var result = await controller.GetFaturaPorMes("outubro", 2026);

        var lancamento = Assert.Single(result.Value!);
        Assert.Equal("Mercado", lancamento.Estabelecimento);
    }

    [Fact]
    public async Task GetFaturaPorMes_MesVazio_RetornaBadRequest()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CartaoLancamentosController(context);

        var result = await controller.GetFaturaPorMes(" ", 2026);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PostLancamento_CriaLancamento()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CartaoLancamentosController(context);

        var result = await controller.PostLancamento(new CartaoLancamento { Estabelecimento = "Mercado" });

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(1, await context.CartaoLancamentos.CountAsync());
    }

    [Fact]
    public async Task DeleteLancamento_RemoveLancamentoExistente()
    {
        using var context = TestDbContextFactory.Create();
        var lancamento = new CartaoLancamento { Estabelecimento = "Mercado" };
        context.CartaoLancamentos.Add(lancamento);
        await context.SaveChangesAsync();
        var controller = new CartaoLancamentosController(context);

        var result = await controller.DeleteLancamento(lancamento.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await context.CartaoLancamentos.FindAsync(lancamento.Id));
    }
}