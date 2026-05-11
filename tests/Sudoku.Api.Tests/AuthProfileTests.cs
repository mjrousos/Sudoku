using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkiaSharp;
using Sudoku.Api;
using Sudoku.Api.Contracts;
using Sudoku.Api.Contracts.Auth;
using Sudoku.Api.Contracts.Me;
using Sudoku.Api.Data;
using Sudoku.Api.Storage;
using Testcontainers.PostgreSql;
using Xunit;

namespace Sudoku.Api.Tests;

public sealed class AuthProfileTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17.6")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.UseSetting("ConnectionStrings:sudokudb", _postgres.GetConnectionString());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IProfilePictureService>();
                    services.AddSingleton<IProfilePictureService, InMemoryProfilePictureService>();
                });
            });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SudokuDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task DevelopmentSignInCreatesCookieSessionAndCurrentUser()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await AddAntiforgeryHeaderAsync(client);

        var signInResponse = await client.PostAsJsonAsync(
            "/api/auth/dev/sign-in",
            new DevelopmentSignInRequest("PlayerOne", "player@example.com", "player-one"));
        var signedInUser = await signInResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();
        var currentUser = await client.GetFromJsonAsync<CurrentUserResponse>("/api/me");

        signInResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        signedInUser.Should().NotBeNull();
        signedInUser!.Username.Should().Be("PlayerOne");
        signedInUser.UsernameConfirmed.Should().BeTrue();
        signedInUser.LinkedLogins.Should().ContainSingle(login => login.Provider == "Development");
        currentUser!.Id.Should().Be(signedInUser.Id);
    }

    [Fact]
    public async Task SignOutClearsAuthenticatedCurrentUser()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await AddAntiforgeryHeaderAsync(client);
        await client.PostAsJsonAsync(
            "/api/auth/dev/sign-in",
            new DevelopmentSignInRequest("SignOutUser", "signout@example.com", "signout-user"));
        await AddAntiforgeryHeaderAsync(client);

        var signOutResponse = await client.PostAsync("/api/auth/sign-out", content: null);
        var meResponse = await client.GetAsync("/api/me");

        signOutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UsernameUpdateValidatesFormatAndUniqueness()
    {
        using var firstClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        using var secondClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await AddAntiforgeryHeaderAsync(firstClient);
        await AddAntiforgeryHeaderAsync(secondClient);
        await firstClient.PostAsJsonAsync(
            "/api/auth/dev/sign-in",
            new DevelopmentSignInRequest("PlayerAlpha", "alpha@example.com", "player-alpha"));
        await secondClient.PostAsJsonAsync(
            "/api/auth/dev/sign-in",
            new DevelopmentSignInRequest("PlayerBeta", "beta@example.com", "player-beta"));
        await AddAntiforgeryHeaderAsync(firstClient);

        var invalidResponse = await firstClient.PutAsJsonAsync("/api/me", new UpdateProfileRequest("no"));
        var duplicateResponse = await firstClient.PutAsJsonAsync("/api/me", new UpdateProfileRequest("PlayerBeta"));
        var validResponse = await firstClient.PutAsJsonAsync("/api/me", new UpdateProfileRequest("Better_Name"));
        var updatedUser = await validResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();

        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        validResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedUser!.Username.Should().Be("Better_Name");
        updatedUser.UsernameConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task UnlinkRejectsRemovingLastLoginMethod()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await AddAntiforgeryHeaderAsync(client);
        await client.PostAsJsonAsync(
            "/api/auth/dev/sign-in",
            new DevelopmentSignInRequest("SoloLogin", "solo@example.com", "solo-login"));
        await AddAntiforgeryHeaderAsync(client);

        var response = await client.DeleteAsync("/api/me/providers/Development");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problem!.Extensions["code"]!.ToString().Should().Be(ApiProblemCodes.LastLoginRemovalNotAllowed);
    }

    [Fact]
    public async Task ProviderListReturnsConfiguredAndLinkedStatus()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var providers = await client.GetFromJsonAsync<IReadOnlyList<AuthProviderResponse>>("/api/auth/providers");

        providers.Should().NotBeNull();
        providers.Should().Contain(provider => provider.Provider == "Google" && !provider.IsConfigured);
        providers.Should().Contain(provider => provider.Provider == "GitHub" && !provider.IsConfigured);
        providers.Should().Contain(provider => provider.Provider == "Facebook" && !provider.IsConfigured);
    }

    [Fact]
    public async Task ProfilePictureUploadProxyAndDeleteUseBackendUrls()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await AddAntiforgeryHeaderAsync(client);
        await client.PostAsJsonAsync(
            "/api/auth/dev/sign-in",
            new DevelopmentSignInRequest("AvatarUser", "avatar@example.com", "avatar-user"));
        await AddAntiforgeryHeaderAsync(client);
        using var form = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent([1, 2, 3, 4]);
        imageContent.Headers.ContentType = new("image/png");
        form.Add(imageContent, "file", "avatar.png");

        var uploadResponse = await client.PostAsync("/api/me/profile-picture", form);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<ProfilePictureResponse>();
        var proxiedPicture = await client.GetAsync(uploaded!.ProfilePictureUrl);
        var deleteResponse = await client.DeleteAsync("/api/me/profile-picture");
        var deleted = await deleteResponse.Content.ReadFromJsonAsync<ProfilePictureResponse>();
        var deletedProxy = await client.GetAsync(uploaded.ProfilePictureUrl);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        uploaded.ProfilePictureUrl.Should().StartWith("/api/users/");
        proxiedPicture.StatusCode.Should().Be(HttpStatusCode.OK);
        proxiedPicture.Headers.CacheControl?.Public.Should().BeTrue();
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deleted!.ProfilePictureUrl.Should().BeNull();
        deletedProxy.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportReturnsAttachmentAndDeleteAccountClearsSession()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await AddAntiforgeryHeaderAsync(client);
        await client.PostAsJsonAsync(
            "/api/auth/dev/sign-in",
            new DevelopmentSignInRequest("ExportUser", "export@example.com", "export-user"));
        await AddAntiforgeryHeaderAsync(client);

        var exportResponse = await client.PostAsync("/api/me/export", content: null);
        var exportJson = await exportResponse.Content.ReadAsStringAsync();
        var deleteResponse = await client.DeleteAsync("/api/me");
        var meAfterDelete = await client.GetAsync("/api/me");

        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        exportResponse.Content.Headers.ContentDisposition?.DispositionType.Should().Be("attachment");
        exportJson.Should().Contain("ExportUser");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        meAfterDelete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void ProfilePictureProcessorNormalizesImagesAndRejectsInvalidContent()
    {
        using var bitmap = new SKBitmap(32, 16);
        bitmap.Erase(SKColors.OrangeRed);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 90);
        using var validInput = new MemoryStream(encoded.ToArray());
        using var invalidInput = new MemoryStream([9, 8, 7]);

        var normalized = ProfilePictureImageProcessor.NormalizeToPng(validInput, 256);
        using var normalizedBitmap = SKBitmap.Decode(normalized);
        var rejectInvalid = () => ProfilePictureImageProcessor.NormalizeToPng(invalidInput, 256);

        normalizedBitmap.Width.Should().Be(256);
        normalizedBitmap.Height.Should().Be(256);
        rejectInvalid.Should().Throw<ProfilePictureException>();
    }

    private static async Task AddAntiforgeryHeaderAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/antiforgery/token");
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token!.RequestToken);
    }

    private sealed class InMemoryProfilePictureService : IProfilePictureService
    {
        private readonly ConcurrentDictionary<string, byte[]> _blobs = new();

        public async Task<string> UploadAsync(Guid userId, IFormFile file, CancellationToken cancellationToken)
        {
            await using var input = file.OpenReadStream();
            using var output = new MemoryStream();
            await input.CopyToAsync(output, cancellationToken);
            var blobName = $"{userId:N}/test.png";
            _blobs[blobName] = output.ToArray();
            return blobName;
        }

        public Task<ProfilePictureReadResult?> OpenReadAsync(string blobName, CancellationToken cancellationToken)
        {
            return Task.FromResult(_blobs.TryGetValue(blobName, out var content)
                ? new ProfilePictureReadResult(new MemoryStream(content), "image/png", DateTimeOffset.UtcNow, "\"test\"")
                : null);
        }

        public Task DeleteAsync(string blobName, CancellationToken cancellationToken)
        {
            _blobs.TryRemove(blobName, out _);
            return Task.CompletedTask;
        }
    }
}
