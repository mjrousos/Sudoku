namespace Sudoku.Api.Storage;

public sealed class ProfilePictureOptions
{
    public const string SectionName = "ProfilePictures";

    public long MaxBytes { get; set; } = 2 * 1024 * 1024;

    public int NormalizedSize { get; set; } = 256;

    public string ContainerName { get; set; } = "profile-pictures";
}
