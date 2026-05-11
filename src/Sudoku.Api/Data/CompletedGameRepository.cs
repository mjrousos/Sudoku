using Microsoft.EntityFrameworkCore;

namespace Sudoku.Api.Data;

public sealed class CompletedGameRepository(SudokuDbContext dbContext) : ICompletedGameRepository
{
    public async Task AddAsync(CompletedGame completedGame, CancellationToken cancellationToken)
    {
        dbContext.CompletedGames.Add(completedGame);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int?> GetAllTimeRankAsync(CompletedGame completedGame, CancellationToken cancellationToken)
    {
        if (!completedGame.QualifiedForLeaderboard)
        {
            return null;
        }

        var betterCompletions = await dbContext.CompletedGames.CountAsync(
            game => game.QualifiedForLeaderboard
                && game.Difficulty == completedGame.Difficulty
                && (game.ElapsedMs < completedGame.ElapsedMs
                    || (game.ElapsedMs == completedGame.ElapsedMs && game.CompletedAt < completedGame.CompletedAt)),
            cancellationToken);

        return betterCompletions + 1;
    }
}
