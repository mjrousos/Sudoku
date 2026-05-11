namespace Sudoku.Api.Contracts.Me;

public sealed record ProfileStatsResponse(
    int TotalGames,
    int CurrentStreakDays,
    IReadOnlyList<DifficultyStatsResponse> ByDifficulty);
