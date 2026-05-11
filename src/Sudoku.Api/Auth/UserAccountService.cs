using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Sudoku.Api.Contracts.Me;
using Sudoku.Api.Data;

namespace Sudoku.Api.Auth;

public sealed class UserAccountService(SudokuDbContext dbContext, TimeProvider timeProvider)
{
    public static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    public static ClaimsPrincipal CreatePrincipal(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public async Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return null;
        }

        return await dbContext.Users
            .Include(user => user.Logins)
            .SingleOrDefaultAsync(user => user.Id == userId.Value, cancellationToken);
    }

    public CurrentUserResponse ToCurrentUserResponse(ApplicationUser user)
    {
        return new CurrentUserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.ProfilePictureBlobName is null ? null : $"/api/users/{user.Id}/profile-picture",
            user.UsernameConfirmed,
            user.Logins
                .OrderBy(login => login.Provider)
                .Select(login => new LinkedLoginResponse(
                    login.Provider,
                    login.DisplayName ?? login.Provider,
                    login.Email,
                    string.Equals(login.Provider, user.PrimaryLoginProvider, StringComparison.OrdinalIgnoreCase)))
                .ToArray());
    }

    public async Task<ApplicationUser> GetOrCreateExternalUserAsync(
        ExternalLoginProfile profile,
        Guid? linkUserId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var provider = AuthProviderRegistry.NormalizeProvider(profile.Provider);
        var existingLogin = await dbContext.UserLogins
            .Include(login => login.User)
            .ThenInclude(user => user.Logins)
            .SingleOrDefaultAsync(
                login => login.Provider == provider && login.ProviderSubjectId == profile.ProviderSubjectId,
                cancellationToken);

        if (existingLogin is not null)
        {
            existingLogin.Email = profile.Email;
            existingLogin.DisplayName = profile.DisplayName;
            existingLogin.User.LastSeenAt = now;
            if (string.IsNullOrWhiteSpace(existingLogin.User.PrimaryLoginProvider))
            {
                existingLogin.User.PrimaryLoginProvider = provider;
            }

            if (IsPrimary(existingLogin.User, provider) && !string.IsNullOrWhiteSpace(profile.Email))
            {
                existingLogin.User.Email = profile.Email;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return existingLogin.User;
        }

        ApplicationUser user;
        if (linkUserId is Guid userId)
        {
            user = await dbContext.Users
                .Include(existingUser => existingUser.Logins)
                .SingleAsync(existingUser => existingUser.Id == userId, cancellationToken);
        }
        else
        {
            user = new ApplicationUser
            {
                Username = await GenerateUniqueUsernameAsync(
                    profile.PreferredUsername ?? profile.DisplayName ?? profile.Email ?? provider,
                    cancellationToken),
                Email = profile.Email,
                PrimaryLoginProvider = provider,
                UsernameConfirmed = false,
                CreatedAt = now,
                LastSeenAt = now
            };
            dbContext.Users.Add(user);
        }

        user.LastSeenAt = now;
        if (string.IsNullOrWhiteSpace(user.PrimaryLoginProvider))
        {
            user.PrimaryLoginProvider = provider;
        }

        user.Logins.Add(new UserLogin
        {
            Provider = provider,
            ProviderSubjectId = profile.ProviderSubjectId,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> IsUsernameAvailableAsync(string username, Guid userId, CancellationToken cancellationToken)
    {
        return !await dbContext.Users.AnyAsync(
            user => user.Id != userId && user.Username == username,
            cancellationToken);
    }

    public async Task<bool> UpdateUsernameAsync(ApplicationUser user, string username, CancellationToken cancellationToken)
    {
        if (!UsernameValidator.IsValid(username)
            || !await IsUsernameAvailableAsync(username, user.Id, cancellationToken))
        {
            return false;
        }

        user.Username = username;
        user.UsernameConfirmed = true;
        user.LastSeenAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetPrimaryProviderAsync(ApplicationUser user, string provider, CancellationToken cancellationToken)
    {
        var normalizedProvider = AuthProviderRegistry.NormalizeProvider(provider);
        var login = user.Logins.FirstOrDefault(candidate =>
            string.Equals(candidate.Provider, normalizedProvider, StringComparison.OrdinalIgnoreCase));
        if (login is null)
        {
            return false;
        }

        user.PrimaryLoginProvider = login.Provider;
        user.Email = login.Email ?? user.Email;
        user.LastSeenAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UnlinkLoginResult> UnlinkProviderAsync(
        ApplicationUser user,
        string provider,
        CancellationToken cancellationToken)
    {
        if (user.Logins.Count <= 1)
        {
            return UnlinkLoginResult.LastLogin;
        }

        var normalizedProvider = AuthProviderRegistry.NormalizeProvider(provider);
        var login = user.Logins.FirstOrDefault(candidate =>
            string.Equals(candidate.Provider, normalizedProvider, StringComparison.OrdinalIgnoreCase));
        if (login is null)
        {
            return UnlinkLoginResult.NotFound;
        }

        dbContext.UserLogins.Remove(login);
        if (IsPrimary(user, login.Provider))
        {
            var nextPrimary = user.Logins.First(candidate => candidate.Id != login.Id);
            user.PrimaryLoginProvider = nextPrimary.Provider;
            user.Email = nextPrimary.Email ?? user.Email;
        }

        user.LastSeenAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return UnlinkLoginResult.Removed;
    }

    private async Task<string> GenerateUniqueUsernameAsync(string seed, CancellationToken cancellationToken)
    {
        var normalized = NormalizeUsernameSeed(seed);
        for (var suffix = 0; suffix < 1_000; suffix++)
        {
            var candidate = suffix == 0
                ? normalized
                : $"{normalized[..Math.Min(normalized.Length, UsernameValidator.MaxLength - suffix.ToString().Length)]}{suffix}";

            if (!await dbContext.Users.AnyAsync(user => user.Username == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return $"player{Guid.NewGuid():N}"[..UsernameValidator.MaxLength];
    }

    private static string NormalizeUsernameSeed(string seed)
    {
        var characters = seed
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-')
            .Take(UsernameValidator.MaxLength)
            .ToArray();
        var candidate = new string(characters);

        if (candidate.Length >= UsernameValidator.MinLength)
        {
            return candidate;
        }

        return $"player{Guid.NewGuid():N}"[..UsernameValidator.MaxLength];
    }

    private static bool IsPrimary(ApplicationUser user, string provider)
    {
        return string.Equals(user.PrimaryLoginProvider, provider, StringComparison.OrdinalIgnoreCase);
    }
}

public enum UnlinkLoginResult
{
    Removed,
    NotFound,
    LastLogin
}
