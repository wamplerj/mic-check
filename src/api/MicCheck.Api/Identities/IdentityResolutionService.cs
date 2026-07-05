using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Identities;

public class IdentityResolutionService(IMicCheckDbContext db)
{
    public async Task<Identity> ResolveAsync(
        int environmentId,
        string identifier,
        IReadOnlyList<TraitInput>? traits,
        CancellationToken ct = default)
    {
        var identity = await db.Identities
            .Include(i => i.Traits)
            .FirstOrDefaultAsync(i => i.EnvironmentId == environmentId && i.Identifier == identifier, ct);

        if (identity is null)
        {
            identity = new Identity
            {
                Identifier = identifier,
                EnvironmentId = environmentId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Identities.Add(identity);
            await db.SaveChangesAsync(ct);
        }

        if (traits is { Count: > 0 })
        {
            foreach (var traitInput in traits)
            {
                var existing = identity.Traits.FirstOrDefault(t => t.Key == traitInput.TraitKey);
                if (existing is not null)
                {
                    existing.Value = traitInput.GetStringValue();
                    existing.ValueType = traitInput.GetValueType();
                }
                else
                {
                    var newTrait = new IdentityTrait
                    {
                        IdentityId = identity.Id,
                        Key = traitInput.TraitKey,
                        Value = traitInput.GetStringValue(),
                        ValueType = traitInput.GetValueType()
                    };
                    db.IdentityTraits.Add(newTrait);
                    identity.Traits.Add(newTrait);
                }
            }
            await db.SaveChangesAsync(ct);
        }

        return identity;
    }
}
