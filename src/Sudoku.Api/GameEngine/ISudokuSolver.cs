namespace Sudoku.Api.GameEngine;

public interface ISudokuSolver
{
    bool IsValidSolution(IReadOnlyList<int> board);

    int CountSolutions(IReadOnlyList<int> puzzle, int limit, CancellationToken cancellationToken = default);

    bool TrySolve(IReadOnlyList<int> puzzle, out int[] solution, CancellationToken cancellationToken = default);
}
