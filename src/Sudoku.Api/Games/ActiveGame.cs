using Sudoku.Api.Contracts;

namespace Sudoku.Api.Games;

public sealed class ActiveGame
{
    public Guid GameId { get; init; }

    public Guid? UserId { get; init; }

    public Difficulty Difficulty { get; init; }

    public required IReadOnlyList<int> Puzzle { get; init; }

    public required IReadOnlyList<int> Solution { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset LastAccessedAt { get; set; }

    public bool DisqualifiedFromLeaderboard { get; set; }

    public int HintCount { get; set; }

    public bool SolutionRevealed { get; set; }
}
