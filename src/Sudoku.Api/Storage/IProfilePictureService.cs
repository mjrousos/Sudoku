namespace Sudoku.Api.Storage;

public interface IProfilePictureService
{
    Task<string> UploadAsync(Guid userId, IFormFile file, CancellationToken cancellationToken);

    Task<ProfilePictureReadResult?> OpenReadAsync(string blobName, CancellationToken cancellationToken);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken);
}
