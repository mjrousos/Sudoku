using Sudoku.Api.Contracts;

namespace Sudoku.Api.Games;

public interface IGameStore
{
    ActiveGame Create(
        Guid? userId,
        Difficulty difficulty,
        IReadOnlyList<int> puzzle,
        IReadOnlyList<int> solution,
        DateTimeOffset startedAt);

    bool TryGet(Guid gameId, out ActiveGame? game);

    bool TryRemove(Guid gameId, out ActiveGame? game);

    int EvictExpired(DateTimeOffset now);
}
