using MicCheck.Api.Tests.Integration.Common;
using Microsoft.AspNetCore.Identity;

namespace MicCheck.Api.Tests.Integration.Auth;

/// <summary>
/// A stand-in for the real API's <c>User</c> type: <see cref="PasswordHasher{TUser}"/> only uses
/// it as a type parameter (it doesn't inspect the instance), so any reference type works. Keeps
/// this project a true black box with no <c>ProjectReference</c> to the API.
/// </summary>
public class HashedUser;

public sealed record AuthSeed(int OrganizationId, int UserId, string Email, string Password)
{
    public static async Task<AuthSeed> InsertAsync(TestDatabase db, string testRunTag, string password)
    {
        var rawUnique = $"{testRunTag}-{Guid.NewGuid():N}";
        var unique = rawUnique[..Math.Min(rawUnique.Length, 40)];
        var email = $"{unique}@integration.test";
        var now = DateTimeOffset.UtcNow;

        var passwordHash = new PasswordHasher<HashedUser>().HashPassword(new HashedUser(), password);

        var organizationId = await db.ExecuteScalarAsync<int>(
            "INSERT INTO \"Organizations\" (\"Name\", \"CreatedAt\") VALUES (@name, @createdAt) RETURNING \"Id\"",
            ("name", $"org_{unique}"),
            ("createdAt", now));

        var userId = await db.ExecuteScalarAsync<int>(
            """
            INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "IsActive", "IsTwoFactorEnabled", "CreatedAt")
            VALUES (@email, @passwordHash, 'Integration', 'Test', true, false, @createdAt)
            RETURNING "Id"
            """,
            ("email", email),
            ("passwordHash", passwordHash),
            ("createdAt", now));

        await db.ExecuteAsync(
            """
            INSERT INTO "OrganizationUsers" ("OrganizationId", "UserId", "Role", "IsPrimary")
            VALUES (@orgId, @userId, 1, true)
            """,
            ("orgId", organizationId),
            ("userId", userId));

        return new AuthSeed(organizationId, userId, email, password);
    }

    public async Task CleanupAsync(TestDatabase db)
    {
        await db.ExecuteAsync("DELETE FROM \"Users\" WHERE \"Id\" = @userId", ("userId", UserId));
        await db.ExecuteAsync("DELETE FROM \"Organizations\" WHERE \"Id\" = @orgId", ("orgId", OrganizationId));
    }
}
