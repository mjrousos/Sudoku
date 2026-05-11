namespace Sudoku.Api.Contracts.Games;

public sealed record GameHistoryPageResponse(
    IReadOnlyList<CompletedGameHistoryResponse> Items,
    int TotalCount);
