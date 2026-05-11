namespace Sudoku.Api.Contracts.Me;

public sealed record DifficultyStatsResponse(
    Difficulty Difficulty,
    int TotalGames,
    long? BestTimeMs,
    double? AverageTimeMs);
