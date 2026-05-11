namespace Sudoku.Api.Data;

public sealed class UserLogin
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public required string Provider { get; set; }

    public required string ProviderSubjectId { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
