using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Sudoku.Api.Storage;

public sealed class BlobProfilePictureService : IProfilePictureService
{
    private const string ContentType = "image/png";
    private readonly BlobContainerClient _containerClient;
    private readonly ProfilePictureOptions _options;

    public BlobProfilePictureService(IConfiguration configuration, IOptions<ProfilePictureOptions> options)
    {
        _options = options.Value;
        var connectionString = configuration.GetConnectionString("profile-pictures")
            ?? configuration.GetConnectionString("storage")
            ?? throw new InvalidOperationException("Blob Storage connection string 'profile-pictures' is not configured.");
        _containerClient = new BlobContainerClient(connectionString, _options.ContainerName);
    }

    public async Task<string> UploadAsync(Guid userId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > _options.MaxBytes)
        {
            throw new ProfilePictureException($"Upload an image up to {_options.MaxBytes / 1024 / 1024} MB.");
        }

        await using var input = file.OpenReadStream();
        var normalized = ProfilePictureImageProcessor.NormalizeToPng(input, _options.NormalizedSize);
        var blobName = $"{userId:N}/{Guid.NewGuid():N}.png";
        var blobClient = _containerClient.GetBlobClient(blobName);

        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        await using var content = new MemoryStream(normalized);
        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = ContentType,
                    CacheControl = "public, max-age=3600"
                }
            },
            cancellationToken);

        return blobName;
    }

    public async Task<ProfilePictureReadResult?> OpenReadAsync(string blobName, CancellationToken cancellationToken)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        try
        {
            var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return new ProfilePictureReadResult(
                download.Value.Content,
                download.Value.Details.ContentType ?? ContentType,
                download.Value.Details.LastModified,
                download.Value.Details.ETag.ToString());
        }
        catch (RequestFailedException exception) when (exception.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        await _containerClient
            .GetBlobClient(blobName)
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
