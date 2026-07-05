using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Data;

public class DatabaseSeeder(MicCheckDbContext db, IPasswordHasher<User> passwordHasher)
{
    /// <summary>
    /// Deterministic credentials for the development/QA seed admin user. Only ever created when
    /// <c>SeedAsync</c> runs, which is gated behind <c>IsDevelopment()</c> in Program.cs.
    /// Used by the API integration suite and the Playwright admin e2e suite to authenticate
    /// without depending on per-run registration.
    /// </summary>
    private const string SeedAdminEmail = "admin@miccheck.local";

    private const string SeedAdminPassword = "MicCheckQa!2026";

    /// <summary>Deterministic environment key for the seeded Development environment, so
    /// HTTP-only integration/e2e tests can read flags without a direct DB connection.</summary>
    private const string SeedDevelopmentEnvironmentKey = "env-qa-development";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Organizations.AnyAsync(ct))
            return;

        var organization = new Organization
        {
            Name = "Default",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(ct);

        var project = new Project
        {
            Name = "My Project",
            OrganizationId = organization.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        var environmentNames = new[] { "Development", "Staging", "Production" };
        foreach (var name in environmentNames)
        {
            db.Environments.Add(new AppEnvironment
            {
                Name = name,
                ApiKey = name == "Development" ? SeedDevelopmentEnvironmentKey : $"env-{Guid.NewGuid():N}",
                ProjectId = project.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);

        var adminUser = new User
        {
            Email = SeedAdminEmail,
            FirstName = "MicCheck",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            PasswordHash = string.Empty
        };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, SeedAdminPassword);
        db.Users.Add(adminUser);
        await db.SaveChangesAsync(ct);

        db.OrganizationUsers.Add(new OrganizationUser
        {
            OrganizationId = organization.Id,
            UserId = adminUser.Id,
            Role = OrganizationRole.Admin,
            IsPrimary = true
        });
        await db.SaveChangesAsync(ct);
    }
}
