namespace Sudoku.Api;

public static class ApiProblemTypes
{
    private const string BaseUri = "https://github.com/mjrousos/Sudoku/problems/";

    public const string GenerationTimeout = BaseUri + "generation-timeout";
    public const string GameSessionLost = BaseUri + "game-session-lost";
    public const string NotImplemented = BaseUri + "not-implemented";
    public const string Validation = BaseUri + "validation";
}
