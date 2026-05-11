using System.Text.RegularExpressions;

namespace Sudoku.Api.Auth;

public static partial class UsernameValidator
{
    public const int MaxLength = 20;
    public const int MinLength = 3;

    public static bool IsValid(string username)
    {
        return UsernameRegex().IsMatch(username);
    }

    [GeneratedRegex("^[a-zA-Z0-9_-]{3,20}$")]
    private static partial Regex UsernameRegex();
}
