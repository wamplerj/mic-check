using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Features;

public class FeatureService(MicCheckDbContext db, AuditService auditService)
{
    private const int MaxFeaturesPerProject = 400;

    public async Task<IReadOnlyList<Feature>> ListByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await db.Features
            .Where(f => f.ProjectId == projectId)
            .ToListAsync(ct);
    }

    public async Task<Feature?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<Feature> CreateAsync(
        int projectId,
        string name,
        FeatureType type,
        string? initialValue,
        string? description,
        CancellationToken ct = default)
    {
        var count = await db.Features.CountAsync(f => f.ProjectId == projectId, ct);
        if (count >= MaxFeaturesPerProject)
            throw new DomainException($"Project has reached the maximum of {MaxFeaturesPerProject} features.");

        var feature = new Feature
        {
            Name = name,
            Type = type,
            InitialValue = initialValue,
            Description = description,
            ProjectId = projectId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Features.Add(feature);
        await db.SaveChangesAsync(ct);

        var environments = await db.Environments
            .Where(e => e.ProjectId == projectId)
            .ToListAsync(ct);

        foreach (var env in environments)
        {
            db.FeatureStates.Add(new FeatureState
            {
                FeatureId = feature.Id,
                EnvironmentId = env.Id,
                Enabled = feature.DefaultEnabled,
                Value = initialValue,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        if (environments.Count > 0)
            await db.SaveChangesAsync(ct);

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project is not null)
            await auditService.LogAsync("Feature", feature.Id.ToString(), "created",
                project.OrganizationId, projectId, ct: ct);

        return feature;
    }

    public async Task<Feature> UpdateAsync(
        int id, string name, string? description, CancellationToken ct = default)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new KeyNotFoundException($"Feature {id} not found.");

        feature.Name = name;
        feature.Description = description;
        await db.SaveChangesAsync(ct);

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == feature.ProjectId, ct);
        if (project is not null)
            await auditService.LogAsync("Feature", feature.Id.ToString(), "updated",
                project.OrganizationId, feature.ProjectId, ct: ct);

        return feature;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null) return;

        db.Features.Remove(feature);
        await db.SaveChangesAsync(ct);
    }
}
