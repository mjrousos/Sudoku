using Sudoku.Api.Contracts;

namespace Sudoku.Api.GameEngine;

public sealed record DifficultyOptions(int MinimumClues, int MaximumClues)
{
    public static DifficultyOptions For(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => new DifficultyOptions(38, 45),
            Difficulty.Medium => new DifficultyOptions(30, 35),
            Difficulty.Hard => new DifficultyOptions(24, 28),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unknown difficulty.")
        };
    }
}
