using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Sudoku.Api.Auth;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddSudokuAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        });

        authenticationBuilder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "__Host-Sudoku.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.LoginPath = "/api/auth/challenge";
            options.AccessDeniedPath = "/api/auth/denied";
            options.Events.OnRedirectToLogin = context => WriteApiStatusOrRedirectAsync(context, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => WriteApiStatusOrRedirectAsync(context, StatusCodes.Status403Forbidden);
        });

        authenticationBuilder.AddCookie(AuthSchemes.External, options =>
        {
            options.Cookie.Name = "Sudoku.External";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });

        AddConfiguredSocialProviders(authenticationBuilder, configuration);
        services.AddAuthorization();
        services.AddScoped<UserAccountService>();
        return services;
    }

    private static void AddConfiguredSocialProviders(AuthenticationBuilder builder, IConfiguration configuration)
    {
        if (AuthProviderRegistry.IsConfigured(configuration, AuthProviderNames.Google))
        {
            builder.AddGoogle(AuthProviderNames.Google, options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
                options.SignInScheme = AuthSchemes.External;
            });
        }

        if (AuthProviderRegistry.IsConfigured(configuration, AuthProviderNames.Facebook))
        {
            builder.AddFacebook(AuthProviderNames.Facebook, options =>
            {
                options.AppId = configuration["Authentication:Facebook:ClientId"]!;
                options.AppSecret = configuration["Authentication:Facebook:ClientSecret"]!;
                options.SignInScheme = AuthSchemes.External;
                options.Fields.Add("email");
            });
        }

        if (AuthProviderRegistry.IsConfigured(configuration, AuthProviderNames.GitHub))
        {
            builder.AddOAuth(AuthProviderNames.GitHub, options =>
            {
                options.ClientId = configuration["Authentication:GitHub:ClientId"]!;
                options.ClientSecret = configuration["Authentication:GitHub:ClientSecret"]!;
                options.CallbackPath = "/signin-github";
                options.SignInScheme = AuthSchemes.External;
                options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                options.UserInformationEndpoint = "https://api.github.com/user";
                options.Scope.Add("read:user");
                options.Scope.Add("user:email");
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                options.ClaimActions.MapJsonKey("urn:github:login", "login");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                options.Events.OnCreatingTicket = async context =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                    using var response = await context.Backchannel.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        context.HttpContext.RequestAborted);
                    response.EnsureSuccessStatusCode();

                    await using var stream = await response.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted);
                    using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: context.HttpContext.RequestAborted);
                    context.RunClaimActions(payload.RootElement);
                };
            });
        }
    }

    private static Task WriteApiStatusOrRedirectAsync(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
