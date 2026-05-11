namespace Sudoku.Api.Contracts.Games;

public sealed record RevealSolutionResponse(IReadOnlyList<int> Solution);
