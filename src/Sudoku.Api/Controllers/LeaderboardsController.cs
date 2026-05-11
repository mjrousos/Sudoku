using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sudoku.Api.Auth;
using Sudoku.Api.Contracts;
using Sudoku.Api.Contracts.Leaderboards;
using Sudoku.Api.Data;

namespace Sudoku.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LeaderboardsController(SudokuDbContext dbContext) : ControllerBase
{
    [HttpGet("{difficulty}/{window}")]
    [AllowAnonymous]
    public async Task<ActionResult<LeaderboardResponse>> Get(
        Difficulty difficulty,
        string window,
        [FromQuery] bool bestPerPlayer = true,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetWindow(window, out var start, out var end))
        {
            return ProblemResult(
                StatusCodes.Status400BadRequest,
                "Leaderboard window is invalid.",
                "Use daily, weekly, or all-time.",
                ApiProblemCodes.ValidationFailed);
        }

        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(0, offset);

        var query = dbContext.CompletedGames
            .Include(game => game.User)
            .Where(game => game.Difficulty == difficulty && game.QualifiedForLeaderboard);

        if (start is not null)
        {
            query = query.Where(game => game.CompletedAt >= start.Value);
        }

        if (end is not null)
        {
            query = query.Where(game => game.CompletedAt < end.Value);
        }

        var rows = await query
            .OrderBy(game => game.ElapsedMs)
            .ThenBy(game => game.CompletedAt)
            .ToArrayAsync(cancellationToken);

        if (bestPerPlayer)
        {
            rows = rows
                .GroupBy(game => game.UserId)
                .Select(group => group
                    .OrderBy(game => game.ElapsedMs)
                    .ThenBy(game => game.CompletedAt)
                    .First())
                .OrderBy(game => game.ElapsedMs)
                .ThenBy(game => game.CompletedAt)
                .ToArray();
        }

        var entries = rows
            .Skip(offset)
            .Take(limit)
            .Select((game, index) => new LeaderboardEntryResponse(
                offset + index + 1,
                game.User.Username,
                game.User.ProfilePictureBlobName is null ? null : $"/api/users/{game.UserId}/profile-picture",
                game.ElapsedMs,
                game.CompletedAt))
            .ToArray();

        var viewerRank = UserAccountService.GetUserId(User) is Guid userId
            ? CalculateViewerRank(rows, userId)
            : null;

        return Ok(new LeaderboardResponse(entries, viewerRank));
    }

    private static int? CalculateViewerRank(IReadOnlyList<CompletedGame> rows, Guid userId)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            if (rows[index].UserId == userId)
            {
                return index + 1;
            }
        }

        return null;
    }

    private static bool TryGetWindow(string window, out DateTimeOffset? start, out DateTimeOffset? end)
    {
        var today = DateTimeOffset.UtcNow.Date;
        switch (window.ToLowerInvariant())
        {
            case "daily":
                start = new DateTimeOffset(today, TimeSpan.Zero);
                end = start.Value.AddDays(1);
                return true;
            case "weekly":
                var offset = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                start = new DateTimeOffset(today.AddDays(-offset), TimeSpan.Zero);
                end = start.Value.AddDays(7);
                return true;
            case "all-time":
                start = null;
                end = null;
                return true;
            default:
                start = null;
                end = null;
                return false;
        }
    }

    private ObjectResult ProblemResult(int statusCode, string title, string detail, string code)
    {
        var problemDetails = new ProblemDetails
        {
            Type = ApiProblemTypes.Validation,
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = HttpContext.Request.Path
        };
        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
