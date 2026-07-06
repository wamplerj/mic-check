using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using MicCheck.Api.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Features;

public class FeatureService(IMicCheckDbContext db, IAuditService auditService, WebhookQueue webhookQueue)
{
    private const int MaxFeaturesPerProject = 400;

    public async Task<IReadOnlyList<Feature>> ListByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await db.Features
            .Include(f => f.Tags)
            .Where(f => f.ProjectId == projectId)
            .ToListAsync(ct);
    }

    public async Task<Feature?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.Features.Include(f => f.Tags).FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<Feature> CreateAsync(int projectId, string name, FeatureType type, string? initialValue, string? description, CancellationToken ct = default)
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
            await auditService.RecordAsync(
                "Feature", feature.Id.ToString(), "created",
                project.OrganizationId, projectId,
                after: new { feature.Name, Type = feature.Type.ToString(), feature.InitialValue, feature.Description },
                ct: ct);

        return feature;
    }

    public async Task<Feature> UpdateAsync(int id, string name, string? description, CancellationToken ct = default)
    {
        var feature = await db.Features.Include(f => f.Tags).FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new KeyNotFoundException($"Feature {id} not found.");

        var before = new { feature.Name, feature.Description };

        feature.Name = name;
        feature.Description = description;
        await db.SaveChangesAsync(ct);

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == feature.ProjectId, ct);
        if (project is not null)
            await auditService.RecordAsync(
                "Feature", feature.Id.ToString(), "updated",
                project.OrganizationId, feature.ProjectId,
                before: before,
                after: new { feature.Name, feature.Description },
                ct: ct);

        return feature;
    }

    public async Task<Feature> AssignTagAsync(int featureId, int tagId, CancellationToken ct = default)
    {
        var feature = await db.Features.Include(f => f.Tags).FirstOrDefaultAsync(f => f.Id == featureId, ct)
            ?? throw new KeyNotFoundException($"Feature {featureId} not found.");
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, ct)
            ?? throw new KeyNotFoundException($"Tag {tagId} not found.");

        if (tag.ProjectId != feature.ProjectId)
            throw new DomainException("Tag does not belong to the feature's project.");

        if (feature.Tags.All(t => t.Id != tagId))
        {
            feature.Tags.Add(tag);
            await db.SaveChangesAsync(ct);
        }

        return feature;
    }

    public async Task<Feature> RemoveTagAsync(int featureId, int tagId, CancellationToken ct = default)
    {
        var feature = await db.Features.Include(f => f.Tags).FirstOrDefaultAsync(f => f.Id == featureId, ct)
            ?? throw new KeyNotFoundException($"Feature {featureId} not found.");

        var tag = feature.Tags.FirstOrDefault(t => t.Id == tagId);
        if (tag is not null)
        {
            feature.Tags.Remove(tag);
            await db.SaveChangesAsync(ct);
        }

        return feature;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null) return;

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == feature.ProjectId, ct);

        db.Features.Remove(feature);
        await db.SaveChangesAsync(ct);

        if (project is not null)
        {
            await auditService.RecordAsync(
                "Feature", id.ToString(), "deleted",
                project.OrganizationId, feature.ProjectId,
                before: new { feature.Name, Type = feature.Type.ToString() },
                ct: ct);

            var environments = await db.Environments.Where(e => e.ProjectId == feature.ProjectId).ToListAsync(ct);
            foreach (var env in environments)
            {
                await webhookQueue.EnqueueAsync(new WebhookEvent
                {
                    EventType = WebhookEventTypes.FlagDeleted,
                    EnvironmentId = env.Id,
                    OrganizationId = project.OrganizationId,
                    Data = new FlagDeletedData(null, DateTimeOffset.UtcNow, new FeatureSummary(feature.Id, feature.Name)) });
            }
        }
    }
}
