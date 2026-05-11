using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sudoku.Api;
using Sudoku.Api.Contracts;
using Sudoku.Api.Contracts.Auth;
using Sudoku.Api.Contracts.Games;
using Sudoku.Api.Contracts.Leaderboards;
using Sudoku.Api.Contracts.Me;
using Sudoku.Api.Data;
using Sudoku.Api.GameEngine;
using Testcontainers.PostgreSql;
using Xunit;

namespace Sudoku.Api.Tests;

public sealed class HistoryLeaderboardTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

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
                    services.RemoveAll<IPuzzleGenerationService>();
                    services.AddSingleton<IPuzzleGenerationService, FixedPuzzleGenerationService>();
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
    public async Task AuthenticatedCompletionPersistsHistoryStatsAndLeaderboardRank()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await AddAntiforgeryHeaderAsync(client);
        await client.PostAsJsonAsync(
            "/api/auth/dev/sign-in",
            new DevelopmentSignInRequest("RankedPlayer", "ranked@example.com", "ranked-player"));
        await AddAntiforgeryHeaderAsync(client);

        var game = await CreateGameAsync(client);
        var completeResponse = await client.PostAsJsonAsync($"/api/games/{game.GameId}/complete", new { board = FixedSolution });
        var completion = await ReadJsonAsync<CompleteGameResponse>(completeResponse);
        var history = await ReadJsonAsync<GameHistoryPageResponse>(await client.GetAsync("/api/me/history"));
        var stats = await ReadJsonAsync<ProfileStatsResponse>(await client.GetAsync("/api/me/stats"));
        var leaderboard = await ReadJsonAsync<LeaderboardResponse>(await client.GetAsync("/api/leaderboards/easy/all-time"));

        completion.LeaderboardRank.Should().Be(1);
        completion.QualifiedForLeaderboard.Should().BeTrue();
        history!.TotalCount.Should().Be(1);
        history.Items[0].QualifiedForLeaderboard.Should().BeTrue();
        stats!.TotalGames.Should().Be(1);
        stats.ByDifficulty.Single(item => item.Difficulty == Difficulty.Easy).TotalGames.Should().Be(1);
        leaderboard!.Entries.Should().ContainSingle(entry => entry.Username == "RankedPlayer" && entry.Rank == 1);
        leaderboard.ViewerRank.Should().Be(1);
    }

    [Fact]
    public async Task HintedCompletionIsPersistedButExcludedFromLeaderboard()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await AddAntiforgeryHeaderAsync(client);
        await client.PostAsJsonAsync(
            "/api/auth/dev/sign-in",
            new DevelopmentSignInRequest("HintedPlayer", "hinted@example.com", "hinted-player"));
        await AddAntiforgeryHeaderAsync(client);

        var game = await CreateGameAsync(client);
        await client.PostAsJsonAsync($"/api/games/{game.GameId}/hint", new { index = 2 });
        var completeResponse = await client.PostAsJsonAsync($"/api/games/{game.GameId}/complete", new { board = FixedSolution });
        var completion = await ReadJsonAsync<CompleteGameResponse>(completeResponse);
        var history = await ReadJsonAsync<GameHistoryPageResponse>(await client.GetAsync("/api/me/history"));
        var leaderboard = await ReadJsonAsync<LeaderboardResponse>(await client.GetAsync("/api/leaderboards/easy/all-time"));

        completion.QualifiedForLeaderboard.Should().BeFalse();
        completion.LeaderboardRank.Should().BeNull();
        history!.Items.Should().ContainSingle(item => !item.QualifiedForLeaderboard && item.HintCount == 1);
        leaderboard!.Entries.Should().BeEmpty();
    }

    private async Task<CreateGameResponse> CreateGameAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/games", new { difficulty = "easy" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadJsonAsync<CreateGameResponse>(response);
    }

    private static async Task AddAntiforgeryHeaderAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/antiforgery/token");
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token!.RequestToken);
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
