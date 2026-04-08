using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Webhooks;

public class WebhookService(MicCheckDbContext db)
{
    public async Task<IReadOnlyList<Webhook>> ListByEnvironmentAsync(int environmentId, CancellationToken ct = default)
    {
        return await db.Webhooks
            .Where(w => w.EnvironmentId == environmentId)
            .ToListAsync(ct);
    }

    public async Task<Webhook?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.Webhooks.FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<Webhook> CreateAsync(int environmentId, string url, string? secret, bool enabled, CancellationToken ct = default)
    {
        var webhook = new Webhook
        {
            Url = url,
            Secret = secret,
            Scope = WebhookScope.Environment,
            EnvironmentId = environmentId,
            Enabled = enabled,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Webhooks.Add(webhook);
        await db.SaveChangesAsync(ct);
        return webhook;
    }

    public async Task<Webhook> UpdateAsync(int id, string url, string? secret, bool enabled, CancellationToken ct = default)
    {
        var webhook = await db.Webhooks.FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new KeyNotFoundException($"Webhook {id} not found.");

        webhook.Url = url;
        webhook.Secret = secret;
        webhook.Enabled = enabled;
        await db.SaveChangesAsync(ct);
        return webhook;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var webhook = await db.Webhooks.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (webhook is null) return;

        db.Webhooks.Remove(webhook);
        await db.SaveChangesAsync(ct);
    }
}
