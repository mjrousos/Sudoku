using Microsoft.Extensions.Options;
using Sudoku.Api.Contracts;

namespace Sudoku.Api.GameEngine;

public sealed class PuzzleGenerationService(
    ISudokuGenerator generator,
    IOptions<PuzzleGenerationOptions> options,
    ILogger<PuzzleGenerationService> logger) : IPuzzleGenerationService
{
    public async Task<GeneratedPuzzle> GenerateAsync(Difficulty difficulty, CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= options.Value.MaximumAttempts; attempt++)
        {
            using var attemptTimeout = new CancellationTokenSource(options.Value.AttemptTimeout);
            using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                attemptTimeout.Token);

            try
            {
                return await Task.Run(() => generator.Generate(difficulty, linkedToken.Token), cancellationToken);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = exception;
                logger.LogWarning(exception, "Sudoku generation attempt {Attempt} timed out for {Difficulty}.", attempt, difficulty);
            }
        }

        throw new PuzzleGenerationException("Sudoku puzzle generation timed out.", lastException);
    }
}
