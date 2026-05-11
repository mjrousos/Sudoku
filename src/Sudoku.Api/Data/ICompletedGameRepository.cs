namespace Sudoku.Api.Data;

public interface ICompletedGameRepository
{
    Task AddAsync(CompletedGame completedGame, CancellationToken cancellationToken);

    Task<int?> GetAllTimeRankAsync(CompletedGame completedGame, CancellationToken cancellationToken);
}
