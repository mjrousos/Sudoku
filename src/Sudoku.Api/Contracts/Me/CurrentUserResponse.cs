namespace Sudoku.Api.Contracts.Me;

public sealed record CurrentUserResponse(
    Guid Id,
    string Username,
    string? Email,
    string? ProfilePictureUrl,
    bool UsernameConfirmed,
    IReadOnlyList<LinkedLoginResponse> LinkedLogins);
