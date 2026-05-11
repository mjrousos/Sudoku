namespace Sudoku.Api.Contracts.Leaderboards;

public sealed record LeaderboardEntryResponse(
    int Rank,
    string Username,
    string? ProfilePictureUrl,
    long ElapsedMs,
    DateTimeOffset CompletedAt);
