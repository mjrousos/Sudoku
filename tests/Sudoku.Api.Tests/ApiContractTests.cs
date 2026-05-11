using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sudoku.Api;
using Sudoku.Api.Contracts;
using Sudoku.Api.Data;
using Xunit;

namespace Sudoku.Api.Tests;

public sealed class ApiContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICompletedGameRepository>();
                services.AddSingleton<ICompletedGameRepository, NoOpCompletedGameRepository>();
            });
        });
    }

    [Fact]
    public async Task StatusEndpointReturnsServerStatus()
    {
        using var client = _factory.CreateClient();

        var status = await client.GetFromJsonAsync<ApiStatusResponse>("/api/status");

        status.Should().NotBeNull();
        status!.Status.Should().Be("ok");
        status.ServerTime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task OpenApiDocumentIsAvailableInDevelopment()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AntiforgeryTokenEndpointReturnsRequestToken()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/antiforgery/token");

        token.Should().NotBeNull();
        token!.RequestToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvalidGameCreateRequestReturnsProblemDetails()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/antiforgery/token");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token!.RequestToken);

        var response = await client.PostAsJsonAsync("/api/games", new { difficulty = "impossible" });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(ApiProblemTypes.Validation);
        problem.Extensions["code"]!.ToString().Should().Be(ApiProblemCodes.ValidationFailed);
    }

    [Fact]
    public async Task ValidGameCreateRequestCreatesGame()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/antiforgery/token");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token!.RequestToken);

        var response = await client.PostAsJsonAsync("/api/games", new { difficulty = "easy" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private sealed class NoOpCompletedGameRepository : ICompletedGameRepository
    {
        public Task AddAsync(CompletedGame completedGame, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<int?> GetAllTimeRankAsync(CompletedGame completedGame, CancellationToken cancellationToken)
        {
            return Task.FromResult<int?>(null);
        }
    }
}
