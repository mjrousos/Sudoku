namespace Sudoku.Api.Games;

public sealed class GameStoreOptions
{
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(2);
}
