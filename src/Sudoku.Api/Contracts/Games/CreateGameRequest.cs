using System.ComponentModel.DataAnnotations;

namespace Sudoku.Api.Contracts.Games;

public sealed record CreateGameRequest
{
    [Required]
    public Difficulty? Difficulty { get; init; }
}
