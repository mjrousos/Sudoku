using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Sudoku.Api.Contracts;

namespace Sudoku.Api.Games;

public sealed class InMemoryGameStore(IOptions<GameStoreOptions> options) : IGameStore
{
    private readonly ConcurrentDictionary<Guid, ActiveGame> _games = [];

    public ActiveGame Create(
        Guid? userId,
        Difficulty difficulty,
        IReadOnlyList<int> puzzle,
        IReadOnlyList<int> solution,
        DateTimeOffset startedAt)
    {
        var game = new ActiveGame
        {
            GameId = Guid.NewGuid(),
            UserId = userId,
            Difficulty = difficulty,
            Puzzle = puzzle.ToArray(),
            Solution = solution.ToArray(),
            StartedAt = startedAt,
            LastAccessedAt = startedAt
        };

        _games[game.GameId] = game;
        return game;
    }

    public bool TryGet(Guid gameId, out ActiveGame? game)
    {
        if (_games.TryGetValue(gameId, out game))
        {
            game.LastAccessedAt = DateTimeOffset.UtcNow;
            return true;
        }

        return false;
    }

    public bool TryRemove(Guid gameId, out ActiveGame? game)
    {
        return _games.TryRemove(gameId, out game);
    }

    public int EvictExpired(DateTimeOffset now)
    {
        var cutoff = now - options.Value.IdleTimeout;
        var removed = 0;

        foreach (var game in _games.Values.Where(game => game.LastAccessedAt < cutoff))
        {
            if (_games.TryRemove(game.GameId, out _))
            {
                removed++;
            }
        }

        return removed;
    }
}
