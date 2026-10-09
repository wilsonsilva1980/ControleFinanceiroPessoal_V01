using ControleFinanceiro.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Tests;

internal static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"controle-financeiro-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}