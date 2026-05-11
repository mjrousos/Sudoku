namespace Sudoku.Api.GameEngine;

public sealed class PuzzleGenerationOptions
{
    public int MaximumAttempts { get; set; } = 3;

    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(3);
}
