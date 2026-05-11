namespace Sudoku.Api.Contracts.Leaderboards;

public sealed record LeaderboardResponse(
    IReadOnlyList<LeaderboardEntryResponse> Entries,
    int? ViewerRank);
