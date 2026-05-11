namespace Sudoku.Api.Contracts.Games;

public sealed record GameMetadataResponse(
    Guid GameId,
    Difficulty Difficulty,
    IReadOnlyList<int> Puzzle,
    DateTimeOffset StartedAt,
    int HintCount,
    bool DisqualifiedFromLeaderboard);
