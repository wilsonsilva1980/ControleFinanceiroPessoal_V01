using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(SignInManager<IdentityUser> signInManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf-token")]
    [ProducesResponseType(typeof(CsrfTokenResponse), StatusCodes.Status200OK)]
    public IActionResult GetCsrfToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new CsrfTokenResponse(tokens.RequestToken!));
    }

    [AllowAnonymous]
    [HttpPost("sign-in")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn(SignInRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await signInManager.PasswordSignInAsync(
            request.UserName,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        return result.Succeeded
            ? NoContent()
            : Unauthorized(new { message = "Invalid username or password." });
    }

    [Authorize]
    [HttpPost("sign-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SignOutOwner(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await signInManager.SignOutAsync();
        return NoContent();
    }
}

/// <summary>Credentials submitted to start an owner session.</summary>
public sealed record SignInRequest
{
    /// <summary>The provisioned owner's username.</summary>
    public required string UserName { get; init; }

    /// <summary>The provisioned owner's password.</summary>
    public required string Password { get; init; }
}

/// <summary>A request token paired with the anti-forgery cookie.</summary>
public sealed record CsrfTokenResponse(string RequestToken);