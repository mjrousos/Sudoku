using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sudoku.Api.Auth;
using Sudoku.Api.Contracts;
using Sudoku.Api.Contracts.Games;
using Sudoku.Api.Contracts.Me;
using Sudoku.Api.Data;

namespace Sudoku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class HistoryController(SudokuDbContext dbContext) : ControllerBase
{
    [HttpGet("history")]
    public async Task<ActionResult<GameHistoryPageResponse>> History(
        [FromQuery] Difficulty? difficulty,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var userId = UserAccountService.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(0, offset);
        var query = dbContext.CompletedGames
            .Where(game => game.UserId == userId.Value);

        if (difficulty is not null)
        {
            query = query.Where(game => game.Difficulty == difficulty);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(game => game.CompletedAt)
            .Skip(offset)
            .Take(limit)
            .Select(game => new CompletedGameHistoryResponse(
                game.Id,
                game.Difficulty,
                game.ElapsedMs,
                game.StartedAt,
                game.CompletedAt,
                game.QualifiedForLeaderboard,
                game.HintCount,
                game.SolutionRevealed))
            .ToArrayAsync(cancellationToken);

        return Ok(new GameHistoryPageResponse(items, totalCount));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ProfileStatsResponse>> Stats(CancellationToken cancellationToken)
    {
        var userId = UserAccountService.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var games = await dbContext.CompletedGames
            .Where(game => game.UserId == userId.Value)
            .ToArrayAsync(cancellationToken);
        var stats = Enum.GetValues<Difficulty>()
            .Select(difficulty =>
            {
                var difficultyGames = games.Where(game => game.Difficulty == difficulty).ToArray();
                var qualifiedGames = difficultyGames.Where(game => game.QualifiedForLeaderboard).ToArray();
                return new DifficultyStatsResponse(
                    difficulty,
                    difficultyGames.Length,
                    qualifiedGames.Length == 0 ? null : qualifiedGames.Min(game => (long)game.ElapsedMs),
                    qualifiedGames.Length == 0 ? null : qualifiedGames.Average(game => game.ElapsedMs));
            })
            .ToArray();

        return Ok(new ProfileStatsResponse(
            games.Length,
            CalculateCurrentStreak(games.Select(game => game.CompletedAt.UtcDateTime.Date).Distinct()),
            stats));
    }

    private static int CalculateCurrentStreak(IEnumerable<DateTime> completionDates)
    {
        var dates = completionDates
            .OrderByDescending(date => date)
            .ToArray();
        if (dates.Length == 0)
        {
            return 0;
        }

        var streak = 1;
        var expected = dates[0].AddDays(-1);
        foreach (var date in dates.Skip(1))
        {
            if (date != expected)
            {
                break;
            }

            streak++;
            expected = expected.AddDays(-1);
        }

        return streak;
    }
}
