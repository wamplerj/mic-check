using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using MicCheck.Api.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Features;

public class FeatureStateService(IMicCheckDbContext db, WebhookQueue webhookQueue, AuditService auditService)
{
    public async Task<IReadOnlyList<FeatureState>> ListByEnvironmentAsync(int environmentId, CancellationToken ct = default)
    {
        return await db.FeatureStates
            .Where(fs => fs.EnvironmentId == environmentId && fs.IdentityId == null && fs.FeatureSegmentId == null)
            .ToListAsync(ct);
    }

    public async Task<FeatureState?> FindByIdAsync(int id, int environmentId, CancellationToken ct = default)
    {
        return await db.FeatureStates
            .FirstOrDefaultAsync(fs => fs.Id == id && fs.EnvironmentId == environmentId, ct);
    }

    public async Task<FeatureState> UpdateAsync(int id, bool enabled, string? value, CancellationToken ct = default)
    {
        var state = await db.FeatureStates.FirstOrDefaultAsync(fs => fs.Id == id, ct)
            ?? throw new KeyNotFoundException($"FeatureState {id} not found.");

        var before = SnapshotState(state);

        state.Enabled = enabled;
        state.Value = value;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        state.Version++;
        await db.SaveChangesAsync(ct);

        await DispatchFlagUpdatedAsync(state, before, ct);
        return state;
    }

    public async Task<FeatureState> PatchAsync(int id, bool? enabled, string? value, CancellationToken ct = default)
    {
        var state = await db.FeatureStates.FirstOrDefaultAsync(fs => fs.Id == id, ct)
            ?? throw new KeyNotFoundException($"FeatureState {id} not found.");

        var before = SnapshotState(state);

        if (enabled.HasValue) state.Enabled = enabled.Value;
        if (value is not null) state.Value = value;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        state.Version++;
        await db.SaveChangesAsync(ct);

        await DispatchFlagUpdatedAsync(state, before, ct);
        return state;
    }

    private async Task DispatchFlagUpdatedAsync(FeatureState state, FeatureStateSnapshot before, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == state.FeatureId, ct);
        var environment = await db.Environments.FirstOrDefaultAsync(e => e.Id == state.EnvironmentId, ct);
        var project = environment is not null
            ? await db.Projects.FirstOrDefaultAsync(p => p.Id == environment.ProjectId, ct)
            : null;

        var featureSummary = feature is not null
            ? new FeatureSummary(feature.Id, feature.Name)
            : new FeatureSummary(state.FeatureId, string.Empty);

        var environmentSummary = environment is not null
            ? new EnvironmentSummary(environment.Id, environment.Name)
            : new EnvironmentSummary(state.EnvironmentId, string.Empty);

        var after = new FlagStateSnapshot(state.Id, state.Enabled, state.Value, featureSummary, environmentSummary);
        var previousSnapshot = new FlagStateSnapshot(state.Id, before.Enabled, before.Value, featureSummary, environmentSummary);

        var data = new FlagUpdatedData(null, DateTimeOffset.UtcNow, after, previousSnapshot);

        await webhookQueue.EnqueueAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            EnvironmentId = state.EnvironmentId,
            OrganizationId = project?.OrganizationId ?? 0,
            Data = data
        });

        if (project is not null)
        {
            await auditService.RecordAsync(
                "FeatureState", state.Id.ToString(), "updated",
                project.OrganizationId, environment!.ProjectId, state.EnvironmentId,
                before: new { before.Enabled, before.Value },
                after: new { state.Enabled, state.Value },
                ct: ct);
        }
    }

    private static FeatureStateSnapshot SnapshotState(FeatureState state) =>
        new(state.Enabled, state.Value);

    private record FeatureStateSnapshot(bool Enabled, string? Value);
}
