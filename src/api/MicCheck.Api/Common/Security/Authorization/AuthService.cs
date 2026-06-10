using System.Security.Cryptography;
using MicCheck.Api.Data;
using MicCheck.Api.Organizations;
using MicCheck.Api.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Common.Security.Authorization;

public class AuthService(
    MicCheckDbContext db,
    ITokenService tokenService,
    IPasswordHasher<User> passwordHasher)
{
    public async Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.Organizations)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

        if (user is null)
            return null;

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        var accessToken = tokenService.GenerateToken(user);
        var refreshTokenValue = GenerateSecureToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        });

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return new LoginResponse(accessToken.Token, refreshTokenValue, accessToken.ExpiresAt);
    }

    public async Task<LoginResponse?> RegisterAsync(
        string email, string password,
        string firstName, string lastName,
        string organizationName,
        CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return null;

        var user = new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            PasswordHash = string.Empty
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var organization = new Organization
        {
            Name = organizationName,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(ct);

        db.OrganizationUsers.Add(new OrganizationUser
        {
            OrganizationId = organization.Id,
            UserId = user.Id,
            Role = OrganizationRole.Admin
        });
        await db.SaveChangesAsync(ct);

        await db.Entry(user).Collection(u => u.Organizations).LoadAsync(ct);

        var accessToken = tokenService.GenerateToken(user);
        var refreshTokenValue = GenerateSecureToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);

        return new LoginResponse(accessToken.Token, refreshTokenValue, accessToken.ExpiresAt);
    }

    public async Task<LoginResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await db.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.Organizations)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked, ct);

        if (token is null || token.ExpiresAt < DateTimeOffset.UtcNow)
            return null;

        token.IsRevoked = true;

        var accessToken = tokenService.GenerateToken(token.User);
        var newRefreshTokenValue = GenerateSecureToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshTokenValue,
            UserId = token.UserId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);

        return new LoginResponse(accessToken.Token, newRefreshTokenValue, accessToken.ExpiresAt);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked, ct);

        if (token is null)
            return;

        token.IsRevoked = true;
        await db.SaveChangesAsync(ct);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
