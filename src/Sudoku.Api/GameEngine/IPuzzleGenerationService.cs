using Sudoku.Api.Contracts;

namespace Sudoku.Api.GameEngine;

public interface IPuzzleGenerationService
{
    Task<GeneratedPuzzle> GenerateAsync(Difficulty difficulty, CancellationToken cancellationToken);
}
