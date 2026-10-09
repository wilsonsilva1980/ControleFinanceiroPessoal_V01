using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ControleFinanceiro.Tests;

public class OwnerAuthenticationTests
{
    [Fact]
    public async Task Provisioner_CreatesHashedOwnerOnlyOnce()
    {
        using var provider = CreateIdentityProvider();
        using var scope = provider.CreateScope();
        var provisioner = new OwnerProvisioningService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OwnerProvisioningOptions
            {
                UserName = "finance-owner",
                Password = "SecureOwner123!"
            }));

        await provisioner.StartAsync(CancellationToken.None);
        await provisioner.StartAsync(CancellationToken.None);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var owner = await userManager.FindByNameAsync("finance-owner");
        Assert.NotNull(owner);
        Assert.True(await userManager.CheckPasswordAsync(owner, "SecureOwner123!"));
        Assert.Single(await userManager.Users.ToListAsync());
    }

    [Fact]
    public async Task Provisioner_CreatesConfiguredOwnerWhenAnotherUserAlreadyExists()
    {
        using var provider = CreateIdentityProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        await userManager.CreateAsync(new IdentityUser { UserName = "existing-owner" }, "ExistingOwner123!");

        var provisioner = new OwnerProvisioningService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OwnerProvisioningOptions
            {
                UserName = "admin",
                Password = "UniqueAdmin456!"
            }));

        await provisioner.StartAsync(CancellationToken.None);

        var admin = await userManager.FindByNameAsync("admin");
        Assert.NotNull(admin);
        Assert.True(await userManager.CheckPasswordAsync(admin, "UniqueAdmin456!"));
        Assert.Equal(2, await userManager.Users.CountAsync());
    }

    [Fact]
    public async Task SignIn_ReturnsSameUnauthorizedResultForInvalidCredentials()
    {
        using var provider = CreateIdentityProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        await userManager.CreateAsync(new IdentityUser { UserName = "finance-owner" }, "SecureOwner123!");

        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
        var controller = new AuthController(scope.ServiceProvider.GetRequiredService<SignInManager<IdentityUser>>())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var validResult = await controller.SignIn(new SignInRequest
        {
            UserName = "finance-owner",
            Password = "SecureOwner123!"
        }, CancellationToken.None);
        var invalidResult = await controller.SignIn(new SignInRequest
        {
            UserName = "finance-owner",
            Password = "wrong-password"
        }, CancellationToken.None);

        Assert.IsType<NoContentResult>(validResult);
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(invalidResult);
        Assert.Equal(
            Assert.IsType<UnauthorizedObjectResult>(
                await controller.SignIn(new SignInRequest
                {
                    UserName = "unknown-owner",
                    Password = "wrong-password"
                }, CancellationToken.None)).Value,
            unauthorized.Value);
    }

    private static ServiceProvider CreateIdentityProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        var databaseName = $"identity-tests-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        return services.BuildServiceProvider();
    }
}