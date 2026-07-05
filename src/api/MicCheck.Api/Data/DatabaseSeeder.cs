using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Data;

public class DatabaseSeeder
{
    /// <summary>
    /// Deterministic credentials for the development/QA seed admin user. Only ever created when
    /// <c>SeedAsync</c> runs, which is gated behind <c>IsDevelopment()</c> in Program.cs.
    /// Used by the API integration suite and the Playwright admin e2e suite to authenticate
    /// without depending on per-run registration.
    /// </summary>
    public const string SeedAdminEmail = "admin@miccheck.local";
    public const string SeedAdminPassword = "MicCheckQa!2026";

    /// <summary>Deterministic environment key for the seeded Development environment, so
    /// HTTP-only integration/e2e tests can read flags without a direct DB connection.</summary>
    public const string SeedDevelopmentEnvironmentKey = "env-qa-development";

    private readonly MicCheckDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public DatabaseSeeder(MicCheckDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Organizations.AnyAsync(ct))
            return;

        var organization = new Organization
        {
            Name = "Default",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync(ct);

        var project = new Project
        {
            Name = "My Project",
            OrganizationId = organization.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        var environmentNames = new[] { "Development", "Staging", "Production" };
        foreach (var name in environmentNames)
        {
            _db.Environments.Add(new AppEnvironment
            {
                Name = name,
                ApiKey = name == "Development" ? SeedDevelopmentEnvironmentKey : $"env-{Guid.NewGuid():N}",
                ProjectId = project.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        var adminUser = new User
        {
            Email = SeedAdminEmail,
            FirstName = "MicCheck",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            PasswordHash = string.Empty
        };
        adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, SeedAdminPassword);
        _db.Users.Add(adminUser);
        await _db.SaveChangesAsync(ct);

        _db.OrganizationUsers.Add(new OrganizationUser
        {
            OrganizationId = organization.Id,
            UserId = adminUser.Id,
            Role = OrganizationRole.Admin,
            IsPrimary = true
        });
        await _db.SaveChangesAsync(ct);
    }
}
