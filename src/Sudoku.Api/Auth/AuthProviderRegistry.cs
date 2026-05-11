namespace Sudoku.Api.Auth;

public sealed record AuthProviderDefinition(string Provider, string DisplayName, string ConfigurationKey);

public static class AuthProviderRegistry
{
    public static IReadOnlyList<AuthProviderDefinition> Providers { get; } =
    [
        new(AuthProviderNames.Google, "Google", "Google"),
        new(AuthProviderNames.GitHub, "GitHub", "GitHub"),
        new(AuthProviderNames.Facebook, "Facebook", "Facebook")
    ];

    public static AuthProviderDefinition? Find(string provider)
    {
        return Providers.FirstOrDefault(definition =>
            string.Equals(definition.Provider, provider, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsConfigured(IConfiguration configuration, string provider)
    {
        var definition = Find(provider);
        return definition is not null
            && !string.IsNullOrWhiteSpace(configuration[$"Authentication:{definition.ConfigurationKey}:ClientId"])
            && !string.IsNullOrWhiteSpace(configuration[$"Authentication:{definition.ConfigurationKey}:ClientSecret"]);
    }

    public static string NormalizeProvider(string provider)
    {
        return Find(provider)?.Provider
            ?? (string.Equals(provider, AuthProviderNames.Development, StringComparison.OrdinalIgnoreCase)
                ? AuthProviderNames.Development
                : provider);
    }
}
