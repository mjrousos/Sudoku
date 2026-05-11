namespace Sudoku.Api.Contracts.Auth;

public sealed record AuthProviderResponse(
    string Provider,
    string DisplayName,
    bool IsConfigured,
    bool IsLinked,
    bool IsPrimary);
