namespace Sudoku.Api.Storage;

public sealed record ProfilePictureReadResult(
    Stream Content,
    string ContentType,
    DateTimeOffset? LastModified,
    string? ETag);
