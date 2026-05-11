namespace Sudoku.Api;

public static class ApiProblemCodes
{
    public const string GenerationTimeout = "generation_timeout";
    public const string GameSessionLost = "game_session_lost";
    public const string InvalidAuthProvider = "invalid_auth_provider";
    public const string LastLoginRemovalNotAllowed = "last_login_removal_not_allowed";
    public const string NotImplemented = "not_implemented";
    public const string ProfilePictureInvalid = "profile_picture_invalid";
    public const string UnexpectedError = "unexpected_error";
    public const string UsernameUnavailable = "username_unavailable";
    public const string ValidationFailed = "validation_failed";
}
