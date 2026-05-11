using Sudoku.Api.Contracts;

namespace Sudoku.Api.GameEngine;

public sealed class SudokuGenerator(ISudokuSolver solver) : ISudokuGenerator
{
    private const int CellCount = 81;

    public GeneratedPuzzle Generate(Difficulty difficulty, CancellationToken cancellationToken)
    {
        var solution = CreateSolvedGrid(cancellationToken);
        var puzzle = solution.ToArray();
        var options = DifficultyOptions.For(difficulty);
        var targetClues = Random.Shared.Next(options.MinimumClues, options.MaximumClues + 1);

        foreach (var index in Enumerable.Range(0, CellCount).OrderBy(_ => Random.Shared.Next()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (CountClues(puzzle) <= targetClues)
            {
                break;
            }

            var removedValue = puzzle[index];
            puzzle[index] = 0;

            if (solver.CountSolutions(puzzle, limit: 2, cancellationToken) != 1)
            {
                puzzle[index] = removedValue;
            }
        }

        return new GeneratedPuzzle(difficulty, puzzle, solution);
    }

    private static int[] CreateSolvedGrid(CancellationToken cancellationToken)
    {
        var board = new int[CellCount];
        if (!FillBoard(board, cancellationToken))
        {
            throw new InvalidOperationException("Unable to generate a solved Sudoku grid.");
        }

        return board;
    }

    private static bool FillBoard(int[] board, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var index = Array.IndexOf(board, 0);
        if (index < 0)
        {
            return true;
        }

        foreach (var value in Enumerable.Range(1, 9).OrderBy(_ => Random.Shared.Next()))
        {
            if (!SudokuSolver.CanPlace(board, index, value))
            {
                continue;
            }

            board[index] = value;
            if (FillBoard(board, cancellationToken))
            {
                return true;
            }
        }

        board[index] = 0;
        return false;
    }

    private static int CountClues(IEnumerable<int> puzzle)
    {
        return puzzle.Count(value => value != 0);
    }
}
