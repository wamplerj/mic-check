using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Common.Security.ApiKeys;

public class ApiKeyService(IMicCheckDbContext db)
{
    public async Task<ApiKeyCreationResult> CreateAsync(
        int organizationId, string name, DateTimeOffset? expiresAt, CancellationToken ct = default)
    {
        var rawKey = ApiKeyHasher.GenerateKey();
        var hashedKey = ApiKeyHasher.Hash(rawKey);
        var prefix = rawKey.Length >= 8 ? rawKey[..8] : rawKey;

        var apiKey = new ApiKey
        {
            Key = hashedKey,
            Prefix = prefix,
            Name = name,
            OrganizationId = organizationId,
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(ct);

        return new ApiKeyCreationResult(apiKey, rawKey);
    }

    public async Task<IReadOnlyList<ApiKey>> ListAsync(int organizationId, CancellationToken ct = default)
    {
        return await db.ApiKeys
            .Where(k => k.OrganizationId == organizationId)
            .ToListAsync(ct);
    }

    public async Task RevokeAsync(int organizationId, int keyId, CancellationToken ct = default)
    {
        var key = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == organizationId, ct);

        if (key is null)
            return;

        key.IsActive = false;
        await db.SaveChangesAsync(ct);
    }
}

public record ApiKeyCreationResult(ApiKey Key, string RawKey);
