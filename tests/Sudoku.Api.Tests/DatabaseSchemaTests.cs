using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sudoku.Api.Contracts;
using Sudoku.Api.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace Sudoku.Api.Tests;

public sealed class DatabaseSchemaTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17.6")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task InitialMigrationCreatesExpectedPersistentTablesOnly()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
            ORDER BY table_name;
            """,
            connection);

        var tableNames = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }

        tableNames.Should().Contain(["users", "user_logins", "completed_games"]);
        tableNames.Should().NotContain("active_games");
    }

    [Fact]
    public async Task UsernameUniquenessIsCaseInsensitive()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new ApplicationUser { Username = "PlayerOne" });
        await dbContext.SaveChangesAsync();

        dbContext.Users.Add(new ApplicationUser { Username = "playerone" });
        var saveDuplicate = () => dbContext.SaveChangesAsync();

        await saveDuplicate.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task CompletedGameDifficultyIsPersistedAsLowercaseText()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser { Username = "PlayerTwo" };
        dbContext.Users.Add(user);
        dbContext.CompletedGames.Add(new CompletedGame
        {
            User = user,
            Difficulty = Difficulty.Medium,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            CompletedAt = DateTimeOffset.UtcNow,
            ElapsedMs = 300_000,
            QualifiedForLeaderboard = true
        });
        await dbContext.SaveChangesAsync();

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT difficulty FROM completed_games LIMIT 1;", connection);
        var difficulty = await command.ExecuteScalarAsync();

        difficulty.Should().Be("medium");
    }

    private SudokuDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SudokuDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new SudokuDbContext(options);
    }
}
