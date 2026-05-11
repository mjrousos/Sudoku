namespace Sudoku.Api.Contracts.Auth;

public sealed record DevelopmentSignInRequest(
    string? Username,
    string? Email,
    string? Subject);
