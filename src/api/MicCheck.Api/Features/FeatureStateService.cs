using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Features;

public class FeatureStateService(MicCheckDbContext db)
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

        state.Enabled = enabled;
        state.Value = value;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        state.Version++;
        await db.SaveChangesAsync(ct);
        return state;
    }

    public async Task<FeatureState> PatchAsync(int id, bool? enabled, string? value, CancellationToken ct = default)
    {
        var state = await db.FeatureStates.FirstOrDefaultAsync(fs => fs.Id == id, ct)
            ?? throw new KeyNotFoundException($"FeatureState {id} not found.");

        if (enabled.HasValue) state.Enabled = enabled.Value;
        if (value is not null) state.Value = value;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        state.Version++;
        await db.SaveChangesAsync(ct);
        return state;
    }
}
