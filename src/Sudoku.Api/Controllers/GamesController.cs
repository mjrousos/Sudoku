using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sudoku.Api.Auth;
using Sudoku.Api.Contracts;
using Sudoku.Api.Contracts.Games;
using Sudoku.Api.Data;
using Sudoku.Api.GameEngine;
using Sudoku.Api.Games;

namespace Sudoku.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class GamesController(
    IPuzzleGenerationService puzzleGenerationService,
    IGameStore gameStore,
    ICompletedGameRepository completedGameRepository,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.GameGeneration)]
    public async Task<ActionResult<CreateGameResponse>> Create(
        CreateGameRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var difficulty = request.Difficulty ?? Difficulty.Easy;
            var puzzle = await puzzleGenerationService.GenerateAsync(difficulty, cancellationToken);
            var startedAt = timeProvider.GetUtcNow();
            var game = gameStore.Create(
                UserAccountService.GetUserId(User),
                difficulty,
                puzzle.Puzzle,
                puzzle.Solution,
                startedAt);

            return CreatedAtAction(
                nameof(Get),
                new { gameId = game.GameId },
                new CreateGameResponse(game.GameId, game.Difficulty, game.Puzzle, game.StartedAt));
        }
        catch (PuzzleGenerationException)
        {
            return ProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                ApiProblemTypes.GenerationTimeout,
                "Puzzle generation timed out.",
                "The server could not generate a puzzle within the time budget. Try again.",
                ApiProblemCodes.GenerationTimeout,
                new Dictionary<string, object?> { ["retryable"] = true });
        }
    }

    [HttpGet("{gameId:guid}")]
    public ActionResult<GameMetadataResponse> Get(Guid gameId)
    {
        if (!gameStore.TryGet(gameId, out var game))
        {
            return GameSessionLost();
        }

        return Ok(ToMetadataResponse(game!));
    }

    [HttpPost("{gameId:guid}/hint")]
    [EnableRateLimiting(RateLimitPolicies.Hints)]
    public ActionResult<HintResponse> Hint(Guid gameId, HintRequest request)
    {
        if (!gameStore.TryGet(gameId, out var game))
        {
            return GameSessionLost();
        }

        var activeGame = game!;

        if (activeGame.Puzzle[request.Index] != 0)
        {
            return ProblemResult(
                StatusCodes.Status400BadRequest,
                ApiProblemTypes.Validation,
                "Hints are only available for empty cells.",
                "Select an empty cell before requesting a hint.",
                ApiProblemCodes.ValidationFailed);
        }

        activeGame.HintCount++;
        activeGame.DisqualifiedFromLeaderboard = true;
        return Ok(new HintResponse(request.Index, activeGame.Solution[request.Index]));
    }

    [HttpPost("{gameId:guid}/reveal")]
    public ActionResult<RevealSolutionResponse> Reveal(Guid gameId)
    {
        if (!gameStore.TryGet(gameId, out var game))
        {
            return GameSessionLost();
        }

        var activeGame = game!;
        activeGame.SolutionRevealed = true;
        activeGame.DisqualifiedFromLeaderboard = true;
        return Ok(new RevealSolutionResponse(activeGame.Solution));
    }

    [HttpPost("{gameId:guid}/complete")]
    public async Task<ActionResult<CompleteGameResponse>> Complete(Guid gameId, CompleteGameRequest request)
    {
        if (!gameStore.TryGet(gameId, out var game))
        {
            return GameSessionLost();
        }

        var activeGame = game!;

        if (request.Board is null || request.Board.Count != 81 || request.Board.Any(value => value is < 0 or > 9))
        {
            return ProblemResult(
                StatusCodes.Status400BadRequest,
                ApiProblemTypes.Validation,
                "The submitted board is invalid.",
                "Submit exactly 81 cells with values from 0 through 9.",
                ApiProblemCodes.ValidationFailed);
        }

        if (!request.Board.SequenceEqual(activeGame.Solution))
        {
            return ProblemResult(
                StatusCodes.Status422UnprocessableEntity,
                ApiProblemTypes.Validation,
                "The submitted board does not solve this puzzle.",
                "Correct the board and try again.",
                ApiProblemCodes.ValidationFailed);
        }

        gameStore.TryRemove(gameId, out _);
        var completedAt = timeProvider.GetUtcNow();
        var elapsedMs = Math.Max(0, (long)(completedAt - activeGame.StartedAt).TotalMilliseconds);
        var qualifiedForLeaderboard = !activeGame.DisqualifiedFromLeaderboard;
        int? leaderboardRank = null;

        if (activeGame.UserId is Guid userId)
        {
            var completedGame = new CompletedGame
            {
                UserId = userId,
                Difficulty = activeGame.Difficulty,
                StartedAt = activeGame.StartedAt,
                CompletedAt = completedAt,
                ElapsedMs = checked((int)elapsedMs),
                HintCount = activeGame.HintCount,
                SolutionRevealed = activeGame.SolutionRevealed,
                QualifiedForLeaderboard = qualifiedForLeaderboard
            };
            await completedGameRepository.AddAsync(completedGame, HttpContext.RequestAborted);
            leaderboardRank = await completedGameRepository.GetAllTimeRankAsync(completedGame, HttpContext.RequestAborted);
        }

        return Ok(new CompleteGameResponse(elapsedMs, completedAt, qualifiedForLeaderboard, leaderboardRank));
    }

    private ActionResult GameSessionLost()
    {
        return ProblemResult(
            StatusCodes.Status404NotFound,
            ApiProblemTypes.GameSessionLost,
            "Game session was not found.",
            "The game may have expired, been completed, or been lost when the server restarted.",
            ApiProblemCodes.GameSessionLost);
    }

    private ActionResult ProblemResult(
        int statusCode,
        string type,
        string title,
        string detail,
        string code,
        IDictionary<string, object?>? extensions = null)
    {
        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = HttpContext.Request.Path
        };
        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions[extension.Key] = extension.Value;
            }
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static GameMetadataResponse ToMetadataResponse(ActiveGame game)
    {
        return new GameMetadataResponse(
            game.GameId,
            game.Difficulty,
            game.Puzzle,
            game.StartedAt,
            game.HintCount,
            game.DisqualifiedFromLeaderboard);
    }
}
