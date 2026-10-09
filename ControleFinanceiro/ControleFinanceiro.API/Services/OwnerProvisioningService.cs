using ControleFinanceiro.API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ControleFinanceiro.API.Services;

public sealed class OwnerProvisioningService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OwnerProvisioningOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var userName = options.Value.UserName;
        var password = options.Value.Password;
        var hasUserName = !string.IsNullOrWhiteSpace(userName);
        var hasPassword = !string.IsNullOrWhiteSpace(password);

        if (!hasUserName && !hasPassword)
        {
            return;
        }

        if (!hasUserName || !hasPassword)
        {
            throw new InvalidOperationException(
                "Owner provisioning requires both OwnerProvisioning:UserName and OwnerProvisioning:Password.");
        }

        var configuredUserName = userName!.Trim();
        var configuredPassword = password!;
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        if (await userManager.FindByNameAsync(configuredUserName) is not null)
        {
            return;
        }

        var owner = new IdentityUser { UserName = configuredUserName };
        var result = await userManager.CreateAsync(owner, configuredPassword);
        if (!result.Succeeded)
        {
            var errorCodes = string.Join(", ", result.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"Owner provisioning failed validation: {errorCodes}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}