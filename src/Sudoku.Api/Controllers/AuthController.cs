using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sudoku.Api.Auth;
using Sudoku.Api.Contracts.Auth;

namespace Sudoku.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    UserAccountService userAccountService) : ControllerBase
{
    [HttpGet("providers")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<AuthProviderResponse>>> Providers(CancellationToken cancellationToken)
    {
        var currentUser = User.Identity?.IsAuthenticated == true
            ? await userAccountService.GetUserAsync(User, cancellationToken)
            : null;

        return Ok(AuthProviderRegistry.Providers
            .Select(provider =>
            {
                var linkedLogin = currentUser?.Logins.FirstOrDefault(login =>
                    string.Equals(login.Provider, provider.Provider, StringComparison.OrdinalIgnoreCase));

                return new AuthProviderResponse(
                    provider.Provider,
                    provider.DisplayName,
                    AuthProviderRegistry.IsConfigured(configuration, provider.Provider),
                    linkedLogin is not null,
                    currentUser is not null
                        && string.Equals(currentUser.PrimaryLoginProvider, provider.Provider, StringComparison.OrdinalIgnoreCase));
            })
            .ToArray());
    }

    [HttpGet("challenge/{provider}")]
    [AllowAnonymous]
    public IActionResult ChallengeProvider(string provider, [FromQuery] string? returnUrl = "/")
    {
        var definition = AuthProviderRegistry.Find(provider);
        if (definition is null)
        {
            return AuthProblem(
                StatusCodes.Status400BadRequest,
                "Unsupported authentication provider.",
                "Choose Google, GitHub, or Facebook.",
                ApiProblemCodes.InvalidAuthProvider);
        }

        if (!AuthProviderRegistry.IsConfigured(configuration, definition.Provider))
        {
            return AuthProblem(
                StatusCodes.Status501NotImplemented,
                "Authentication provider is not configured.",
                $"{definition.DisplayName} sign-in needs client credentials before it can be used.",
                ApiProblemCodes.NotImplemented);
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(ExternalCallback), new { returnUrl = SafeReturnUrl(returnUrl) })
        };
        properties.Items["provider"] = definition.Provider;

        var linkUserId = UserAccountService.GetUserId(User);
        if (linkUserId is Guid userId)
        {
            properties.Items["linkUserId"] = userId.ToString();
        }

        return Challenge(properties, definition.Provider);
    }

    [HttpGet("external-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalCallback([FromQuery] string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(AuthSchemes.External);
        if (!result.Succeeded || result.Principal is null)
        {
            return AuthProblem(
                StatusCodes.Status401Unauthorized,
                "External sign-in failed.",
                "The external provider did not return a usable identity.",
                ApiProblemCodes.ValidationFailed);
        }

        var provider = result.Properties?.Items.TryGetValue("provider", out var providerValue) == true
            ? providerValue
            : null;
        var subject = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(subject))
        {
            return AuthProblem(
                StatusCodes.Status401Unauthorized,
                "External sign-in failed.",
                "The external provider did not return the required account identifiers.",
                ApiProblemCodes.ValidationFailed);
        }

        Guid? linkUserId = null;
        if (result.Properties?.Items.TryGetValue("linkUserId", out var linkUserIdValue) == true
            && Guid.TryParse(linkUserIdValue, out var parsedUserId))
        {
            linkUserId = parsedUserId;
        }

        var user = await userAccountService.GetOrCreateExternalUserAsync(
            BuildExternalProfile(provider, subject, result.Principal),
            linkUserId,
            cancellationToken);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            UserAccountService.CreatePrincipal(user));
        await HttpContext.SignOutAsync(AuthSchemes.External);

        return LocalRedirect(SafeReturnUrl(returnUrl));
    }

    [HttpPost("sign-out")]
    [Authorize]
    public async Task<IActionResult> SignOutCurrentUser()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPost("dev/sign-in")]
    [AllowAnonymous]
    public async Task<ActionResult> DevelopmentSignIn(
        DevelopmentSignInRequest request,
        CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            return NotFound();
        }

        var username = string.IsNullOrWhiteSpace(request.Username)
            ? "dev-player"
            : request.Username;
        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? username
            : request.Subject;

        var user = await userAccountService.GetOrCreateExternalUserAsync(
            new ExternalLoginProfile(
                AuthProviderNames.Development,
                subject,
                request.Email,
                username,
                username),
            linkUserId: null,
            cancellationToken);
        await userAccountService.UpdateUsernameAsync(user, user.Username, cancellationToken);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            UserAccountService.CreatePrincipal(user));

        return Ok(userAccountService.ToCurrentUserResponse(user));
    }

    private static ExternalLoginProfile BuildExternalProfile(string provider, string subject, ClaimsPrincipal principal)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var displayName = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("urn:github:login")
            ?? email;
        var preferredUsername = principal.FindFirstValue("urn:github:login")
            ?? email?.Split('@')[0]
            ?? displayName;

        return new ExternalLoginProfile(
            provider,
            subject,
            email,
            displayName,
            preferredUsername);
    }

    private string SafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/";
    }

    private ObjectResult AuthProblem(int statusCode, string title, string detail, string code)
    {
        var problemDetails = new ProblemDetails
        {
            Type = ApiProblemTypes.Validation,
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = HttpContext.Request.Path
        };
        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
