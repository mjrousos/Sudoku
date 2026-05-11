namespace Sudoku.Api.Contracts;

public sealed record ApiStatusResponse(string Status, DateTimeOffset ServerTime);
