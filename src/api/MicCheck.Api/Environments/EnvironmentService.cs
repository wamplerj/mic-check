using MicCheck.Api.Common.Security.ApiKeys;
using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using MicCheck.Api.Features;
using Microsoft.EntityFrameworkCore;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Environments;

public class EnvironmentService(MicCheckDbContext db, AuditService auditService)
{
    public async Task<IReadOnlyList<AppEnvironment>> ListByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await db.Environments
            .Where(e => e.ProjectId == projectId)
            .ToListAsync(ct);
    }

    public async Task<AppEnvironment?> FindByApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        return await db.Environments.FirstOrDefaultAsync(e => e.ApiKey == apiKey, ct);
    }

    public async Task<AppEnvironment> CreateAsync(int projectId, string name, CancellationToken ct = default)
    {
        var apiKey = ApiKeyHasher.GenerateKey();

        var environment = new AppEnvironment
        {
            Name = name,
            ApiKey = apiKey,
            ProjectId = projectId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Environments.Add(environment);
        await db.SaveChangesAsync(ct);

        var features = await db.Features
            .Where(f => f.ProjectId == projectId)
            .ToListAsync(ct);

        foreach (var feature in features)
        {
            db.FeatureStates.Add(new FeatureState
            {
                FeatureId = feature.Id,
                EnvironmentId = environment.Id,
                Enabled = feature.DefaultEnabled,
                Value = feature.InitialValue,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        if (features.Count > 0)
            await db.SaveChangesAsync(ct);

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project is not null)
            await auditService.LogAsync("Environment", environment.Id.ToString(), "created",
                project.OrganizationId, projectId, environment.Id, ct: ct);

        return environment;
    }

    public async Task<AppEnvironment> UpdateAsync(string apiKey, string name, CancellationToken ct = default)
    {
        var environment = await db.Environments.FirstOrDefaultAsync(e => e.ApiKey == apiKey, ct)
            ?? throw new KeyNotFoundException($"Environment with key '{apiKey}' not found.");

        environment.Name = name;
        await db.SaveChangesAsync(ct);

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == environment.ProjectId, ct);
        if (project is not null)
            await auditService.LogAsync("Environment", environment.Id.ToString(), "updated",
                project.OrganizationId, environment.ProjectId, environment.Id, ct: ct);

        return environment;
    }

    public async Task DeleteAsync(string apiKey, CancellationToken ct = default)
    {
        var environment = await db.Environments.FirstOrDefaultAsync(e => e.ApiKey == apiKey, ct);
        if (environment is null) return;

        db.Environments.Remove(environment);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AppEnvironment> CloneAsync(string sourceApiKey, string newName, CancellationToken ct = default)
    {
        var source = await db.Environments.FirstOrDefaultAsync(e => e.ApiKey == sourceApiKey, ct)
            ?? throw new KeyNotFoundException($"Source environment with key '{sourceApiKey}' not found.");

        var newApiKey = ApiKeyHasher.GenerateKey();

        var cloned = new AppEnvironment
        {
            Name = newName,
            ApiKey = newApiKey,
            ProjectId = source.ProjectId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Environments.Add(cloned);
        await db.SaveChangesAsync(ct);

        var sourceStates = await db.FeatureStates
            .Where(fs => fs.EnvironmentId == source.Id && fs.IdentityId == null && fs.FeatureSegmentId == null)
            .ToListAsync(ct);

        foreach (var state in sourceStates)
        {
            db.FeatureStates.Add(new FeatureState
            {
                FeatureId = state.FeatureId,
                EnvironmentId = cloned.Id,
                Enabled = state.Enabled,
                Value = state.Value,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        if (sourceStates.Count > 0)
            await db.SaveChangesAsync(ct);

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == source.ProjectId, ct);
        if (project is not null)
            await auditService.LogAsync("Environment", cloned.Id.ToString(), "cloned",
                project.OrganizationId, source.ProjectId, cloned.Id, ct: ct);

        return cloned;
    }
}
