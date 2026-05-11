using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sudoku.Api.Auth;
using Sudoku.Api.Contracts.Me;
using Sudoku.Api.Data;
using Sudoku.Api.Storage;

namespace Sudoku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController(
    UserAccountService userAccountService,
    SudokuDbContext dbContext,
    IProfilePictureService profilePictureService) : ControllerBase
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [HttpGet]
    public async Task<ActionResult<CurrentUserResponse>> Get(CancellationToken cancellationToken)
    {
        var user = await userAccountService.GetUserAsync(User, cancellationToken);
        return user is null ? Unauthorized() : Ok(userAccountService.ToCurrentUserResponse(user));
    }

    [HttpPut]
    public async Task<ActionResult<CurrentUserResponse>> Update(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userAccountService.GetUserAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!UsernameValidator.IsValid(request.Username))
        {
            return ProfileProblem(
                StatusCodes.Status400BadRequest,
                "Username is invalid.",
                "Usernames must be 3-20 characters and may contain letters, numbers, underscores, and hyphens.",
                ApiProblemCodes.ValidationFailed);
        }

        if (!await userAccountService.UpdateUsernameAsync(user, request.Username, cancellationToken))
        {
            return ProfileProblem(
                StatusCodes.Status409Conflict,
                "Username is unavailable.",
                "Choose a different public username.",
                ApiProblemCodes.UsernameUnavailable);
        }

        return Ok(userAccountService.ToCurrentUserResponse(user));
    }

    [HttpPost("profile-picture")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<ProfilePictureResponse>> UploadProfilePicture(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var user = await userAccountService.GetUserAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (file is null)
        {
            return ProfileProblem(
                StatusCodes.Status400BadRequest,
                "Profile picture is required.",
                "Upload a PNG, JPEG, or WebP image file.",
                ApiProblemCodes.ProfilePictureInvalid);
        }

        try
        {
            var oldBlobName = user.ProfilePictureBlobName;
            var newBlobName = await profilePictureService.UploadAsync(user.Id, file, cancellationToken);
            user.ProfilePictureBlobName = newBlobName;
            await dbContext.SaveChangesAsync(cancellationToken);

            if (oldBlobName is not null)
            {
                await profilePictureService.DeleteAsync(oldBlobName, cancellationToken);
            }

            return Ok(new ProfilePictureResponse(userAccountService.ToCurrentUserResponse(user).ProfilePictureUrl));
        }
        catch (ProfilePictureException exception)
        {
            return ProfileProblem(
                StatusCodes.Status400BadRequest,
                "Profile picture is invalid.",
                exception.Message,
                ApiProblemCodes.ProfilePictureInvalid);
        }
    }

    [HttpDelete("profile-picture")]
    public async Task<ActionResult<ProfilePictureResponse>> DeleteProfilePicture(CancellationToken cancellationToken)
    {
        var user = await userAccountService.GetUserAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var oldBlobName = user.ProfilePictureBlobName;
        user.ProfilePictureBlobName = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (oldBlobName is not null)
        {
            await profilePictureService.DeleteAsync(oldBlobName, cancellationToken);
        }

        return Ok(new ProfilePictureResponse(null));
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var userId = UserAccountService.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await dbContext.Users
            .Include(account => account.Logins)
            .Include(account => account.CompletedGames)
            .SingleOrDefaultAsync(account => account.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var export = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            profile = userAccountService.ToCurrentUserResponse(user),
            completedGames = user.CompletedGames
                .OrderByDescending(game => game.CompletedAt)
                .Select(game => new
                {
                    game.Id,
                    game.Difficulty,
                    game.StartedAt,
                    game.CompletedAt,
                    game.ElapsedMs,
                    game.HintCount,
                    game.SolutionRevealed,
                    game.QualifiedForLeaderboard
                })
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(export, ExportJsonOptions);
        return File(bytes, "application/json", $"sudoku-export-{user.Id:N}.json");
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        var user = await userAccountService.GetUserAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var oldBlobName = user.ProfilePictureBlobName;
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (oldBlobName is not null)
        {
            await profilePictureService.DeleteAsync(oldBlobName, cancellationToken);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPost("providers/primary")]
    public async Task<ActionResult<CurrentUserResponse>> SetPrimaryProvider(
        SetPrimaryLoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userAccountService.GetUserAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!await userAccountService.SetPrimaryProviderAsync(user, request.Provider, cancellationToken))
        {
            return ProfileProblem(
                StatusCodes.Status404NotFound,
                "Linked provider was not found.",
                "Link the provider before setting it as primary.",
                ApiProblemCodes.InvalidAuthProvider);
        }

        return Ok(userAccountService.ToCurrentUserResponse(user));
    }

    [HttpDelete("providers/{provider}")]
    public async Task<ActionResult<CurrentUserResponse>> UnlinkProvider(
        string provider,
        CancellationToken cancellationToken)
    {
        var user = await userAccountService.GetUserAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await userAccountService.UnlinkProviderAsync(user, provider, cancellationToken);
        return result switch
        {
            UnlinkLoginResult.Removed => Ok(userAccountService.ToCurrentUserResponse(user)),
            UnlinkLoginResult.LastLogin => ProfileProblem(
                StatusCodes.Status400BadRequest,
                "Cannot unlink the last sign-in provider.",
                "Link another provider before removing this one.",
                ApiProblemCodes.LastLoginRemovalNotAllowed),
            _ => ProfileProblem(
                StatusCodes.Status404NotFound,
                "Linked provider was not found.",
                "Choose a provider linked to this account.",
                ApiProblemCodes.InvalidAuthProvider)
        };
    }

    private ObjectResult ProfileProblem(int statusCode, string title, string detail, string code)
    {
        var problemDetails = new ProblemDetails
        {
            Type = ApiProblemTypes.Validation,
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = HttpContext.Request.Path
        };
        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
