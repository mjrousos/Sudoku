using Sudoku.Api.Contracts;

namespace Sudoku.Api.Data;

public sealed class CompletedGame
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Difficulty Difficulty { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset CompletedAt { get; set; }

    public int ElapsedMs { get; set; }

    public int HintCount { get; set; }

    public bool SolutionRevealed { get; set; }

    public bool QualifiedForLeaderboard { get; set; }
}
