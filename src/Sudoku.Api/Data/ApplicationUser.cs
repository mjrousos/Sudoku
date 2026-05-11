using Sudoku.Api.Contracts;

namespace Sudoku.Api.Data;

public sealed class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Username { get; set; }

    public string? Email { get; set; }

    public string? ProfilePictureBlobName { get; set; }

    public bool UsernameConfirmed { get; set; } = true;

    public string? PrimaryLoginProvider { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<UserLogin> Logins { get; } = [];

    public ICollection<CompletedGame> CompletedGames { get; } = [];
}
