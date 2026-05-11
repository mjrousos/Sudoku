namespace Sudoku.Api.Contracts.Games;

public sealed record CompleteGameRequest(IReadOnlyList<int> Board);
