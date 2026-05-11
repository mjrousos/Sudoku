namespace Sudoku.Api.Contracts.Games;

public sealed record CompleteGameResponse(
    long ElapsedMs,
    DateTimeOffset CompletedAt,
    bool QualifiedForLeaderboard,
    int? LeaderboardRank);
