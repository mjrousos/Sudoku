namespace Sudoku.Api.Contracts.Me;

public sealed record LinkedLoginResponse(
    string Provider,
    string DisplayName,
    string? Email,
    bool IsPrimary);
