using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sudoku.Api.Data;
using Sudoku.Api.Storage;

namespace Sudoku.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController(
    SudokuDbContext dbContext,
    IProfilePictureService profilePictureService) : ControllerBase
{
    [HttpGet("{userId:guid}/profile-picture")]
    [AllowAnonymous]
    public async Task<IActionResult> ProfilePicture(Guid userId, CancellationToken cancellationToken)
    {
        var blobName = await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.ProfilePictureBlobName)
            .SingleOrDefaultAsync(cancellationToken);
        if (blobName is null)
        {
            return NotFound();
        }

        var picture = await profilePictureService.OpenReadAsync(blobName, cancellationToken);
        if (picture is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=3600";
        if (picture.ETag is not null)
        {
            Response.Headers.ETag = picture.ETag;
        }

        if (picture.LastModified is not null)
        {
            Response.Headers.LastModified = picture.LastModified.Value.ToString("R");
        }

        return File(picture.Content, picture.ContentType);
    }
}
