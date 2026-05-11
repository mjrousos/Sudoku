namespace Sudoku.Api.Contracts.Games;

public sealed record CompletedGameHistoryResponse(
    Guid Id,
    Difficulty Difficulty,
    long ElapsedMs,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    bool QualifiedForLeaderboard,
    int HintCount,
    bool SolutionRevealed);
