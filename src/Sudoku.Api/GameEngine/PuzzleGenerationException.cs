namespace Sudoku.Api.GameEngine;

public sealed class PuzzleGenerationException : Exception
{
    public PuzzleGenerationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
