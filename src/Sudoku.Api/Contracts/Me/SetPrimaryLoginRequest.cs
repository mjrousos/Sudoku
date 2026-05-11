using System.ComponentModel.DataAnnotations;

namespace Sudoku.Api.Contracts.Me;

public sealed record SetPrimaryLoginRequest(
    [Required]
    string Provider);
