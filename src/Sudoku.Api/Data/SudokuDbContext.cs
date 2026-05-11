using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sudoku.Api.Contracts;

namespace Sudoku.Api.Data;

public sealed class SudokuDbContext(DbContextOptions<SudokuDbContext> options) : DbContext(options)
{
    private static readonly ValueConverter<Difficulty, string> DifficultyConverter = new(
        difficulty => difficulty.ToString().ToLowerInvariant(),
        value => Enum.Parse<Difficulty>(value, ignoreCase: true));

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public DbSet<UserLogin> UserLogins => Set<UserLogin>();

    public DbSet<CompletedGame> CompletedGames => Set<CompletedGame>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.Username)
                .HasColumnName("username")
                .HasColumnType("citext")
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(user => user.Email).HasColumnName("email");
            entity.Property(user => user.ProfilePictureBlobName).HasColumnName("profile_picture_blob_name");
            entity.Property(user => user.UsernameConfirmed)
                .HasColumnName("username_confirmed")
                .HasDefaultValue(true);
            entity.Property(user => user.PrimaryLoginProvider)
                .HasColumnName("primary_login_provider")
                .HasMaxLength(64);
            entity.Property(user => user.CreatedAt).HasColumnName("created_at");
            entity.Property(user => user.LastSeenAt).HasColumnName("last_seen_at");
            entity.HasIndex(user => user.Username).IsUnique();
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.ToTable("user_logins");
            entity.HasKey(login => login.Id);
            entity.Property(login => login.Id).HasColumnName("id");
            entity.Property(login => login.UserId).HasColumnName("user_id");
            entity.Property(login => login.Provider)
                .HasColumnName("provider")
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(login => login.ProviderSubjectId)
                .HasColumnName("provider_subject_id")
                .HasMaxLength(256)
                .IsRequired();
            entity.Property(login => login.Email)
                .HasColumnName("email")
                .HasMaxLength(320);
            entity.Property(login => login.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(256);
            entity.Property(login => login.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(login => new { login.Provider, login.ProviderSubjectId }).IsUnique();
            entity.HasOne(login => login.User)
                .WithMany(user => user.Logins)
                .HasForeignKey(login => login.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompletedGame>(entity =>
        {
            entity.ToTable("completed_games");
            entity.HasKey(game => game.Id);
            entity.Property(game => game.Id).HasColumnName("id");
            entity.Property(game => game.UserId).HasColumnName("user_id");
            entity.Property(game => game.Difficulty)
                .HasColumnName("difficulty")
                .HasConversion(DifficultyConverter)
                .HasMaxLength(16)
                .IsRequired();
            entity.Property(game => game.StartedAt).HasColumnName("started_at");
            entity.Property(game => game.CompletedAt).HasColumnName("completed_at");
            entity.Property(game => game.ElapsedMs).HasColumnName("elapsed_ms");
            entity.Property(game => game.HintCount).HasColumnName("hint_count");
            entity.Property(game => game.SolutionRevealed).HasColumnName("solution_revealed");
            entity.Property(game => game.QualifiedForLeaderboard).HasColumnName("qualified_for_leaderboard");
            entity.HasIndex(game => new
            {
                game.Difficulty,
                game.QualifiedForLeaderboard,
                game.CompletedAt,
                game.ElapsedMs
            });
            entity.HasOne(game => game.User)
                .WithMany(user => user.CompletedGames)
                .HasForeignKey(game => game.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
