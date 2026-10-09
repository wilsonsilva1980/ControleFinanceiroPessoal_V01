using System.Net;
using System.Net.Http.Json;
using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace ControleFinanceiro.Tests;

public class AuthenticationPipelineTests
{
    [Fact]
    public void DebtWorkbookImportService_IsRegisteredInApplicationServices()
    {
        using var factory = new FinanceApiFactory();
        using var scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IDebtWorkbookImportService>();

        Assert.IsType<DebtWorkbookImportService>(service);
    }

    [Fact]
    public async Task AnonymousFinancialRequest_ReturnsUnauthorized()
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateClient(CreateClientOptions());

        var response = await client.GetAsync("/api/entradas");
        var accountsResponse = await client.GetAsync("/api/BankAccounts");
        var cardsResponse = await client.GetAsync("/api/CreditCards");
        var tripBudgetsResponse = await client.GetAsync("/api/TripBudgets");
        using var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new ByteArrayContent([]), "file", "anonymous.xlsx");
        var workbookPreviewResponse = await client.PostAsync("/api/Debts/import/preview", uploadContent);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, accountsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, cardsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, tripBudgetsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, workbookPreviewResponse.StatusCode);
    }

    [Fact]
    public async Task DevelopmentSwaggerIsAvailableToAnonymousClients()
    {
        using var factory = new FinanceApiFactory("Development");
        using var client = factory.CreateClient(CreateClientOptions());

        var swaggerResponse = await client.GetAsync("/swagger");
        var documentResponse = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, swaggerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, documentResponse.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedOwnerCanReadAndUnsafeRequestRequiresAntiForgeryToken()
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateClient(CreateClientOptions());
        await CreateOwnerAsync(factory);
        await CreateFinancialResourcesAsync(factory);

        var tokenResponse = await client.GetAsync("/api/auth/csrf-token");
        tokenResponse.EnsureSuccessStatusCode();
        var tokenBody = await tokenResponse.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        Assert.NotNull(tokenBody);
        var cookies = new Dictionary<string, string>(StringComparer.Ordinal);
        StoreCookies(client, tokenResponse, cookies);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", tokenBody.RequestToken);

        var knownUserWithWrongPassword = await client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            userName = "finance-owner",
            password = "wrong-password"
        });
        var unknownUser = await client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            userName = "unknown-owner",
            password = "wrong-password"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, knownUserWithWrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.Equal(
            await knownUserWithWrongPassword.Content.ReadAsStringAsync(),
            await unknownUser.Content.ReadAsStringAsync());

        var signInResponse = await client.PostAsJsonAsync("/api/auth/sign-in", new SignInRequest
        {
            UserName = "finance-owner",
            Password = "SecureOwner123!"
        });
        Assert.Equal(HttpStatusCode.NoContent, signInResponse.StatusCode);
        StoreCookies(client, signInResponse, cookies);

        var protectedResponse = await client.GetAsync("/api/entradas");
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
        Assert.Contains(signInResponse.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.StartsWith("ControleFinanceiro.Auth=", StringComparison.Ordinal)
            && cookie.Contains("; secure", StringComparison.OrdinalIgnoreCase));

        var authenticatedTokenResponse = await client.GetAsync("/api/auth/csrf-token");
        authenticatedTokenResponse.EnsureSuccessStatusCode();
        var authenticatedToken = await authenticatedTokenResponse.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        Assert.NotNull(authenticatedToken);
        StoreCookies(client, authenticatedTokenResponse, cookies);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", authenticatedToken.RequestToken);

        var legacyCreateResponse = await client.PostAsJsonAsync("/api/entradas", new
        {
            dataPrevista = "2026-10-15",
            descricao = "Test income",
            categoria = "Salário",
            valorPrevisto = 100m,
            status = "Pendente"
        });
        Assert.Equal(HttpStatusCode.Created, legacyCreateResponse.StatusCode);

        var accountsResponse = await client.GetAsync("/api/BankAccounts");
        var cardsResponse = await client.GetAsync("/api/CreditCards");
        Assert.Equal(HttpStatusCode.OK, accountsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cardsResponse.StatusCode);
        var accountJson = await accountsResponse.Content.ReadAsStringAsync();
        Assert.Contains("***23", accountJson, StringComparison.Ordinal);
        Assert.DoesNotContain("00123", accountJson, StringComparison.Ordinal);
        Assert.DoesNotContain("99887766", accountJson, StringComparison.Ordinal);

        var tripResponse = await client.PostAsJsonAsync("/api/TripBudgets", new
        {
            name = "Test trip",
            destination = "Santiago",
            clpPerBrlQuote = 200.0m
        });
        Assert.True(
            tripResponse.StatusCode == HttpStatusCode.Created,
            $"Trip budget request returned {(int)tripResponse.StatusCode}: {await tripResponse.Content.ReadAsStringAsync()}");

        var recurringResponse = await client.PostAsJsonAsync("/api/RecurringExpenses", new
        {
            name = "Annual service",
            chargeType = "Serviço",
            amount = 75m,
            paymentType = "Cartão",
            cadence = "Yearly",
            initialRenewalDate = "2026-10-15"
        });
        Assert.Equal(HttpStatusCode.Created, recurringResponse.StatusCode);
        using var recurringJson = System.Text.Json.JsonDocument.Parse(await recurringResponse.Content.ReadAsStringAsync());
        var recurringId = recurringJson.RootElement.GetProperty("id").GetInt32();

        var billResponse = await client.PostAsJsonAsync("/api/ContasCasa", new
        {
            dataVencimento = "2026-10-15",
            descricao = "Annual service ledger entry",
            categoria = "Serviços",
            valorPrevisto = 75m,
            valorPago = 75m,
            status = "Pago"
        });
        Assert.Equal(HttpStatusCode.Created, billResponse.StatusCode);
        using var billJson = System.Text.Json.JsonDocument.Parse(await billResponse.Content.ReadAsStringAsync());
        var billId = billJson.RootElement.GetProperty("id").GetInt32();

        var octoberProjectionResponse = await client.GetAsync("/api/RecurringExpenses/projection/monthly?year=2026&month=10");
        octoberProjectionResponse.EnsureSuccessStatusCode();
        using var octoberProjection = System.Text.Json.JsonDocument.Parse(await octoberProjectionResponse.Content.ReadAsStringAsync());
        Assert.Equal(75m, octoberProjection.RootElement.GetProperty("scheduledAmount").GetDecimal());
        var occurrenceId = octoberProjection.RootElement.GetProperty("occurrences")[0].GetProperty("id").GetInt32();

        var linkResponse = await client.PostAsJsonAsync(
            $"/api/RecurringExpenses/occurrences/{occurrenceId}/link",
            new { houseBillId = billId });
        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);
        var duplicateRecurringLinkResponse = await client.PostAsJsonAsync(
            $"/api/RecurringExpenses/occurrences/{occurrenceId}/link",
            new { houseBillId = billId });
        Assert.Equal(HttpStatusCode.Conflict, duplicateRecurringLinkResponse.StatusCode);

        var debtRequest = new
        {
            description = "Linked annual debt",
            dueDate = "2026-10-15T00:00:00Z",
            originalAmount = 75m,
            installments = new[]
            {
                new
                {
                    dueDate = "2026-10-15T00:00:00Z",
                    scheduledAmount = 75m,
                    linkedHouseBillId = billId
                }
            }
        };
        var debtResponse = await client.PostAsJsonAsync("/api/Debts", debtRequest);
        Assert.Equal(HttpStatusCode.Created, debtResponse.StatusCode);
        var duplicateDebtLinkResponse = await client.PostAsJsonAsync("/api/Debts", debtRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateDebtLinkResponse.StatusCode);

        var monthlySummaryResponse = await client.GetAsync("/api/Debts/summary/monthly?year=2026&month=10");
        monthlySummaryResponse.EnsureSuccessStatusCode();
        using var monthlySummary = System.Text.Json.JsonDocument.Parse(await monthlySummaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(75m, monthlySummary.RootElement.GetProperty("scheduledAmount").GetDecimal());
        Assert.Equal(75m, monthlySummary.RootElement.GetProperty("cashOutflowAmount").GetDecimal());

        var novemberProjectionResponse = await client.GetAsync("/api/RecurringExpenses/projection/monthly?year=2026&month=11");
        novemberProjectionResponse.EnsureSuccessStatusCode();
        using var novemberProjection = System.Text.Json.JsonDocument.Parse(await novemberProjectionResponse.Content.ReadAsStringAsync());
        Assert.Empty(novemberProjection.RootElement.GetProperty("occurrences").EnumerateArray());

        var yearlySummaryResponse = await client.GetAsync("/api/Debts/summary/yearly?year=2026");
        yearlySummaryResponse.EnsureSuccessStatusCode();
        using var yearlySummary = System.Text.Json.JsonDocument.Parse(await yearlySummaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(75m, yearlySummary.RootElement.GetProperty("cashOutflowAmount").GetDecimal());
        Assert.Equal(75m, yearlySummary.RootElement.GetProperty("scheduledAmount").GetDecimal());

        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var mutationResponse = await client.PostAsJsonAsync("/api/entradas", new { descricao = "test" });
        using var uploadWithoutToken = new MultipartFormDataContent();
        uploadWithoutToken.Add(new ByteArrayContent([]), "file", "missing-token.xlsx");
        var uploadMutationResponse = await client.PostAsync("/api/Debts/import/preview", uploadWithoutToken);
        Assert.Equal(HttpStatusCode.BadRequest, mutationResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, uploadMutationResponse.StatusCode);
    }

    private static WebApplicationFactoryClientOptions CreateClientOptions() => new()
    {
        BaseAddress = new Uri("https://localhost")
    };

    private static async Task CreateOwnerAsync(FinanceApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var result = await userManager.CreateAsync(
            new IdentityUser { UserName = "finance-owner" },
            "SecureOwner123!");

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Code)));
    }

    private static async Task CreateFinancialResourcesAsync(FinanceApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.BankAccounts.Add(new BankAccount
        {
            BankName = "Example Bank",
            AccountName = "Household",
            AccountType = "Checking",
            BranchIdentifier = "00123",
            AccountIdentifier = "99887766",
            Balance = 0,
            OverdraftUsed = 25,
            OverdraftLimit = 100
        });
        context.CreditCards.Add(new CreditCard
        {
            Issuer = "Example Issuer",
            DisplayName = "Daily card",
            PurchaseLimit = 1000,
            DueDay = 12
        });
        await context.SaveChangesAsync();
    }

    private static void StoreCookies(
        HttpClient client,
        HttpResponseMessage response,
        IDictionary<string, string> cookies)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var setCookie in setCookies)
            {
                var cookiePair = setCookie.Split(';', 2)[0].Split('=', 2);
                cookies[cookiePair[0]] = cookiePair.Length > 1 ? cookiePair[1] : string.Empty;
            }
        }

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add(
            "Cookie",
            string.Join("; ", cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
    }

    private sealed class FinanceApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _environment;

        public FinanceApiFactory(string environment = "Testing")
        {
            _environment = environment;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                var databaseName = $"api-auth-tests-{Guid.NewGuid()}";
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        }
    }
}