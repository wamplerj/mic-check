using MicCheck.Api.Data;
using MicCheck.Api.Features;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Identities;

public class AdminIdentityService(MicCheckDbContext db)
{
    public async Task<(int Total, IReadOnlyList<Identity> Items)> ListAsync(
        int environmentId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Identities
            .Include(i => i.Traits)
            .Where(i => i.EnvironmentId == environmentId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (total, items);
    }

    public async Task<Identity?> CreateAsync(int environmentId, string identifier, CancellationToken ct = default)
    {
        var existing = await db.Identities
            .FirstOrDefaultAsync(i => i.EnvironmentId == environmentId && i.Identifier == identifier, ct);

        if (existing is not null)
            return null;

        var identity = new Identity
        {
            Identifier = identifier,
            EnvironmentId = environmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Identities.Add(identity);
        await db.SaveChangesAsync(ct);
        return identity;
    }

    public async Task<Identity?> FindByIdAsync(int id, int environmentId, CancellationToken ct = default)
    {
        return await db.Identities
            .Include(i => i.Traits)
            .FirstOrDefaultAsync(i => i.Id == id && i.EnvironmentId == environmentId, ct);
    }

    public async Task<TraitResponse?> UpsertTraitAsync(
        int identityId, int environmentId, string key, string value, CancellationToken ct = default)
    {
        var identity = await db.Identities
            .Include(i => i.Traits)
            .FirstOrDefaultAsync(i => i.Id == identityId && i.EnvironmentId == environmentId, ct);

        if (identity is null) return null;

        var existing = identity.Traits.FirstOrDefault(t => t.Key == key);
        if (existing is not null)
        {
            existing.Value = value;
        }
        else
        {
            var trait = new IdentityTrait
            {
                IdentityId = identityId,
                Key = key,
                Value = value,
                ValueType = TraitValueType.String,
            };
            db.IdentityTraits.Add(trait);
        }

        await db.SaveChangesAsync(ct);
        return new TraitResponse(key, value);
    }

    public async Task<bool> DeleteTraitAsync(
        int identityId, int environmentId, string key, CancellationToken ct = default)
    {
        var identity = await db.Identities
            .Include(i => i.Traits)
            .FirstOrDefaultAsync(i => i.Id == identityId && i.EnvironmentId == environmentId, ct);

        if (identity is null) return false;

        var trait = identity.Traits.FirstOrDefault(t => t.Key == key);
        if (trait is null) return false;

        db.IdentityTraits.Remove(trait);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var identity = await db.Identities.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (identity is null) return;

        db.Identities.Remove(identity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<FeatureState>> GetFeatureStatesAsync(
        int identityId, int environmentId, CancellationToken ct = default)
    {
        return await db.FeatureStates
            .Where(fs => fs.IdentityId == identityId && fs.EnvironmentId == environmentId)
            .ToListAsync(ct);
    }

    public async Task<FeatureState> SetFeatureStateAsync(
        int identityId, int environmentId, int featureId, bool enabled, string? value, CancellationToken ct = default)
    {
        var state = await db.FeatureStates
            .FirstOrDefaultAsync(fs => fs.IdentityId == identityId && fs.EnvironmentId == environmentId && fs.FeatureId == featureId, ct);

        if (state is not null)
        {
            state.Enabled = enabled;
            state.Value = value;
            state.UpdatedAt = DateTimeOffset.UtcNow;
            state.Version++;
        }
        else
        {
            state = new FeatureState
            {
                FeatureId = featureId,
                EnvironmentId = environmentId,
                IdentityId = identityId,
                Enabled = enabled,
                Value = value,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.FeatureStates.Add(state);
        }

        await db.SaveChangesAsync(ct);
        return state;
    }

    public async Task DeleteFeatureStateAsync(
        int identityId, int environmentId, int featureId, CancellationToken ct = default)
    {
        var state = await db.FeatureStates
            .FirstOrDefaultAsync(fs => fs.IdentityId == identityId && fs.EnvironmentId == environmentId && fs.FeatureId == featureId, ct);

        if (state is null) return;

        db.FeatureStates.Remove(state);
        await db.SaveChangesAsync(ct);
    }
}
