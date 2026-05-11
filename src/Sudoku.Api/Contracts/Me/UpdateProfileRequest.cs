using System.ComponentModel.DataAnnotations;

namespace Sudoku.Api.Contracts.Me;

public sealed record UpdateProfileRequest(
    [Required]
    string Username);
