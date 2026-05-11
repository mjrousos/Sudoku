namespace Sudoku.Api.Auth;

public sealed record ExternalLoginProfile(
    string Provider,
    string ProviderSubjectId,
    string? Email,
    string? DisplayName,
    string? PreferredUsername);
