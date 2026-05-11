using Microsoft.Extensions.Options;
using FluentAssertions;
using Sudoku.Api.Contracts;
using Sudoku.Api.Games;
using Xunit;

namespace Sudoku.Api.Tests;

public sealed class GameStoreTests
{
    [Fact]
    public void StoreCreatesRetrievesAndRemovesActiveGames()
    {
        var store = CreateStore();
        var startedAt = DateTimeOffset.UtcNow;

        var game = store.Create(null, Difficulty.Easy, EmptyBoard(), SolvedBoard(), startedAt);

        store.TryGet(game.GameId, out var retrieved).Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.StartedAt.Should().Be(startedAt);
        store.TryRemove(game.GameId, out var removed).Should().BeTrue();
        removed.Should().BeSameAs(retrieved);
        store.TryGet(game.GameId, out _).Should().BeFalse();
    }

    [Fact]
    public void StoreEvictsIdleGames()
    {
        var store = CreateStore(TimeSpan.FromMinutes(5));
        var game = store.Create(null, Difficulty.Easy, EmptyBoard(), SolvedBoard(), DateTimeOffset.UtcNow.AddMinutes(-10));
        game.LastAccessedAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var removed = store.EvictExpired(DateTimeOffset.UtcNow);

        removed.Should().Be(1);
        store.TryGet(game.GameId, out _).Should().BeFalse();
    }

    private static InMemoryGameStore CreateStore(TimeSpan? idleTimeout = null)
    {
        return new InMemoryGameStore(Options.Create(new GameStoreOptions
        {
            IdleTimeout = idleTimeout ?? TimeSpan.FromHours(2)
        }));
    }

    private static int[] EmptyBoard()
    {
        return new int[81];
    }

    private static int[] SolvedBoard()
    {
        return
        [
            1, 2, 3, 4, 5, 6, 7, 8, 9,
            4, 5, 6, 7, 8, 9, 1, 2, 3,
            7, 8, 9, 1, 2, 3, 4, 5, 6,
            2, 3, 4, 5, 6, 7, 8, 9, 1,
            5, 6, 7, 8, 9, 1, 2, 3, 4,
            8, 9, 1, 2, 3, 4, 5, 6, 7,
            3, 4, 5, 6, 7, 8, 9, 1, 2,
            6, 7, 8, 9, 1, 2, 3, 4, 5,
            9, 1, 2, 3, 4, 5, 6, 7, 8
        ];
    }
}
