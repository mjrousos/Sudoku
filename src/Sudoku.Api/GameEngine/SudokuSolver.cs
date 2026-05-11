namespace Sudoku.Api.GameEngine;

public sealed class SudokuSolver : ISudokuSolver
{
    private const int CellCount = 81;
    private const int GridSize = 9;

    public bool IsValidSolution(IReadOnlyList<int> board)
    {
        if (board.Count != CellCount || board.Any(value => value is < 1 or > 9))
        {
            return false;
        }

        for (var i = 0; i < GridSize; i++)
        {
            if (!ContainsAllDigits(GetRow(board, i)) || !ContainsAllDigits(GetColumn(board, i)))
            {
                return false;
            }
        }

        for (var boxRow = 0; boxRow < GridSize; boxRow += 3)
        {
            for (var boxColumn = 0; boxColumn < GridSize; boxColumn += 3)
            {
                if (!ContainsAllDigits(GetBox(board, boxRow, boxColumn)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public int CountSolutions(IReadOnlyList<int> puzzle, int limit, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ValidatePuzzleShape(puzzle);

        var board = puzzle.ToArray();
        if (!GivensAreConsistent(board))
        {
            return 0;
        }

        var solutionCount = 0;
        CountSolutions(board, limit, ref solutionCount, cancellationToken);
        return solutionCount;
    }

    public bool TrySolve(IReadOnlyList<int> puzzle, out int[] solution, CancellationToken cancellationToken = default)
    {
        ValidatePuzzleShape(puzzle);

        var board = puzzle.ToArray();
        if (!GivensAreConsistent(board))
        {
            solution = [];
            return false;
        }

        if (Solve(board, cancellationToken))
        {
            solution = board;
            return true;
        }

        solution = [];
        return false;
    }

    internal static bool CanPlace(IReadOnlyList<int> board, int index, int value)
    {
        var row = index / GridSize;
        var column = index % GridSize;
        var boxRow = row / 3 * 3;
        var boxColumn = column / 3 * 3;

        for (var i = 0; i < GridSize; i++)
        {
            if (board[row * GridSize + i] == value || board[i * GridSize + column] == value)
            {
                return false;
            }
        }

        for (var currentRow = boxRow; currentRow < boxRow + 3; currentRow++)
        {
            for (var currentColumn = boxColumn; currentColumn < boxColumn + 3; currentColumn++)
            {
                if (board[currentRow * GridSize + currentColumn] == value)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool Solve(int[] board, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var index = FindEmptyCellWithFewestCandidates(board, out var candidates);
        if (index < 0)
        {
            return true;
        }

        foreach (var candidate in candidates)
        {
            board[index] = candidate;
            if (Solve(board, cancellationToken))
            {
                return true;
            }
        }

        board[index] = 0;
        return false;
    }

    private static void CountSolutions(int[] board, int limit, ref int solutionCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (solutionCount >= limit)
        {
            return;
        }

        var index = FindEmptyCellWithFewestCandidates(board, out var candidates);
        if (index < 0)
        {
            solutionCount++;
            return;
        }

        foreach (var candidate in candidates)
        {
            board[index] = candidate;
            CountSolutions(board, limit, ref solutionCount, cancellationToken);
            if (solutionCount >= limit)
            {
                break;
            }
        }

        board[index] = 0;
    }

    private static int FindEmptyCellWithFewestCandidates(IReadOnlyList<int> board, out int[] candidates)
    {
        var bestIndex = -1;
        var bestCandidates = Array.Empty<int>();

        for (var index = 0; index < CellCount; index++)
        {
            if (board[index] != 0)
            {
                continue;
            }

            var currentCandidates = Enumerable.Range(1, 9)
                .Where(candidate => CanPlace(board, index, candidate))
                .ToArray();

            if (currentCandidates.Length == 0)
            {
                candidates = [];
                return index;
            }

            if (bestIndex < 0 || currentCandidates.Length < bestCandidates.Length)
            {
                bestIndex = index;
                bestCandidates = currentCandidates;
            }
        }

        candidates = bestCandidates;
        return bestIndex;
    }

    private static void ValidatePuzzleShape(IReadOnlyList<int> puzzle)
    {
        if (puzzle.Count != CellCount)
        {
            throw new ArgumentException("A Sudoku board must contain exactly 81 cells.", nameof(puzzle));
        }

        if (puzzle.Any(value => value is < 0 or > 9))
        {
            throw new ArgumentException("Sudoku board values must be between 0 and 9.", nameof(puzzle));
        }
    }

    private static bool GivensAreConsistent(int[] board)
    {
        for (var index = 0; index < board.Length; index++)
        {
            var value = board[index];
            if (value == 0)
            {
                continue;
            }

            board[index] = 0;
            var canPlace = CanPlace(board, index, value);
            board[index] = value;

            if (!canPlace)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<int> GetRow(IReadOnlyList<int> board, int row)
    {
        for (var column = 0; column < GridSize; column++)
        {
            yield return board[row * GridSize + column];
        }
    }

    private static IEnumerable<int> GetColumn(IReadOnlyList<int> board, int column)
    {
        for (var row = 0; row < GridSize; row++)
        {
            yield return board[row * GridSize + column];
        }
    }

    private static IEnumerable<int> GetBox(IReadOnlyList<int> board, int boxRow, int boxColumn)
    {
        for (var row = boxRow; row < boxRow + 3; row++)
        {
            for (var column = boxColumn; column < boxColumn + 3; column++)
            {
                yield return board[row * GridSize + column];
            }
        }
    }

    private static bool ContainsAllDigits(IEnumerable<int> values)
    {
        return values.Order().SequenceEqual(Enumerable.Range(1, 9));
    }
}
