using Sudoku.Api.Contracts;

namespace Sudoku.Api.GameEngine;

public interface ISudokuGenerator
{
    GeneratedPuzzle Generate(Difficulty difficulty, CancellationToken cancellationToken);
}
