namespace Sudoku.Api.Contracts.Games;

public sealed record CreateGameResponse(
    Guid GameId,
    Difficulty Difficulty,
    IReadOnlyList<int> Puzzle,
    DateTimeOffset StartedAt);
