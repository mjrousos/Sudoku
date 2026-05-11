using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sudoku.Api;
using Sudoku.Api.Contracts;
using Sudoku.Api.Contracts.Games;
using Sudoku.Api.Data;
using Sudoku.Api.GameEngine;
using Xunit;

namespace Sudoku.Api.Tests;

public sealed class GameApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public async Task CreateThenGetReturnsGameMetadataWithoutSolution()
    {
        using var client = CreateClient();
        await AddAntiforgeryHeaderAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/games", new { difficulty = "easy" });
        var createdGame = await ReadJsonAsync<CreateGameResponse>(createResponse);
        var metadataResponse = await client.GetAsync($"/api/games/{createdGame.GameId}");
        var metadata = await ReadJsonAsync<GameMetadataResponse>(metadataResponse);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createdGame.Puzzle.Should().Equal(FixedPuzzle);
        metadata.Should().NotBeNull();
        metadata!.Puzzle.Should().Equal(FixedPuzzle);
        metadata.HintCount.Should().Be(0);
        metadata.DisqualifiedFromLeaderboard.Should().BeFalse();
    }

    [Fact]
    public async Task HintReturnsCellValueAndDisqualifiesGame()
    {
        using var client = CreateClient();
        await AddAntiforgeryHeaderAsync(client);
        var game = await CreateGameAsync(client);

        var hintResponse = await client.PostAsJsonAsync($"/api/games/{game.GameId}/hint", new { index = 2 });
        var hint = await ReadJsonAsync<HintResponse>(hintResponse);
        var metadataResponse = await client.GetAsync($"/api/games/{game.GameId}");
        var metadata = await ReadJsonAsync<GameMetadataResponse>(metadataResponse);

        hintResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        hint.Should().BeEquivalentTo(new HintResponse(2, FixedSolution[2]));
        metadata!.HintCount.Should().Be(1);
        metadata.DisqualifiedFromLeaderboard.Should().BeTrue();
    }

    [Fact]
    public async Task RevealReturnsSolutionAndDisqualifiesGame()
    {
        using var client = CreateClient();
        await AddAntiforgeryHeaderAsync(client);
        var game = await CreateGameAsync(client);

        var revealResponse = await client.PostAsync($"/api/games/{game.GameId}/reveal", content: null);
        var reveal = await ReadJsonAsync<RevealSolutionResponse>(revealResponse);
        var metadataResponse = await client.GetAsync($"/api/games/{game.GameId}");
        var metadata = await ReadJsonAsync<GameMetadataResponse>(metadataResponse);

        revealResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        reveal!.Solution.Should().Equal(FixedSolution);
        metadata!.DisqualifiedFromLeaderboard.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteRejectsIncorrectBoardAndKeepsGameActive()
    {
        using var client = CreateClient();
        await AddAntiforgeryHeaderAsync(client);
        var game = await CreateGameAsync(client);
        var wrongBoard = FixedSolution.ToArray();
        wrongBoard[0] = 9;

        var completeResponse = await client.PostAsJsonAsync($"/api/games/{game.GameId}/complete", new { board = wrongBoard });
        var problem = await ReadJsonAsync<ProblemDetails>(completeResponse);
        var metadataResponse = await client.GetAsync($"/api/games/{game.GameId}");

        completeResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        problem!.Extensions["code"]!.ToString().Should().Be(ApiProblemCodes.ValidationFailed);
        metadataResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CompleteAcceptsCorrectBoardAndRemovesGame()
    {
        using var client = CreateClient();
        await AddAntiforgeryHeaderAsync(client);
        var game = await CreateGameAsync(client);

        var completeResponse = await client.PostAsJsonAsync($"/api/games/{game.GameId}/complete", new { board = FixedSolution });
        var completion = await ReadJsonAsync<CompleteGameResponse>(completeResponse);
        var getAfterCompletion = await client.GetAsync($"/api/games/{game.GameId}");

        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        completion!.ElapsedMs.Should().BeGreaterThanOrEqualTo(0);
        completion.QualifiedForLeaderboard.Should().BeTrue();
        getAfterCompletion.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static HttpClient CreateClient()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPuzzleGenerationService>();
                    services.RemoveAll<ICompletedGameRepository>();
                    services.AddSingleton<IPuzzleGenerationService, FixedPuzzleGenerationService>();
                    services.AddSingleton<ICompletedGameRepository, NoOpCompletedGameRepository>();
                });
            });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    private static async Task AddAntiforgeryHeaderAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/antiforgery/token");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token!.RequestToken);
    }

    private static async Task<CreateGameResponse> CreateGameAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/games", new { difficulty = "easy" });
        response.EnsureSuccessStatusCode();
        return await ReadJsonAsync<CreateGameResponse>(response);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    private sealed class FixedPuzzleGenerationService : IPuzzleGenerationService
    {
        public Task<GeneratedPuzzle> GenerateAsync(Difficulty difficulty, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GeneratedPuzzle(difficulty, FixedPuzzle, FixedSolution));
        }
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

    private static readonly int[] FixedPuzzle =
    [
        5, 3, 0, 0, 7, 0, 0, 0, 0,
        6, 0, 0, 1, 9, 5, 0, 0, 0,
        0, 9, 8, 0, 0, 0, 0, 6, 0,
        8, 0, 0, 0, 6, 0, 0, 0, 3,
        4, 0, 0, 8, 0, 3, 0, 0, 1,
        7, 0, 0, 0, 2, 0, 0, 0, 6,
        0, 6, 0, 0, 0, 0, 2, 8, 0,
        0, 0, 0, 4, 1, 9, 0, 0, 5,
        0, 0, 0, 0, 8, 0, 0, 7, 9
    ];

    private static readonly int[] FixedSolution =
    [
        5, 3, 4, 6, 7, 8, 9, 1, 2,
        6, 7, 2, 1, 9, 5, 3, 4, 8,
        1, 9, 8, 3, 4, 2, 5, 6, 7,
        8, 5, 9, 7, 6, 1, 4, 2, 3,
        4, 2, 6, 8, 5, 3, 7, 9, 1,
        7, 1, 3, 9, 2, 4, 8, 5, 6,
        9, 6, 1, 5, 3, 7, 2, 8, 4,
        2, 8, 7, 4, 1, 9, 6, 3, 5,
        3, 4, 5, 2, 8, 6, 1, 7, 9
    ];
}
