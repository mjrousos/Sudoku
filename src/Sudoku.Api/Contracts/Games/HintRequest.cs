using System.ComponentModel.DataAnnotations;

namespace Sudoku.Api.Contracts.Games;

public sealed record HintRequest
{
    [Range(0, 80)]
    public int Index { get; init; }
}
