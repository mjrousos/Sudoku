using Sudoku.Api.Contracts;

namespace Sudoku.Api.GameEngine;

public sealed record GeneratedPuzzle(
    Difficulty Difficulty,
    IReadOnlyList<int> Puzzle,
    IReadOnlyList<int> Solution);
