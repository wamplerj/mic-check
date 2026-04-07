using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using Microsoft.EntityFrameworkCore;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Data;

public class DatabaseSeeder
{
    private readonly MicCheckDbContext _db;

    public DatabaseSeeder(MicCheckDbContext db)
    {
        _db = db;
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
                ApiKey = $"env-{Guid.NewGuid():N}",
                ProjectId = project.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
