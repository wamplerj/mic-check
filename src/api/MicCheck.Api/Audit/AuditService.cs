using System.Text.Json;
using MicCheck.Api.Data;
using MicCheck.Api.Webhooks;

namespace MicCheck.Api.Audit;

public class AuditService(MicCheckDbContext db, IHttpContextAccessor httpContextAccessor, WebhookQueue webhookQueue)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public virtual async Task RecordAsync(
        string resourceType,
        string resourceId,
        string action,
        int organizationId,
        int? projectId = null,
        int? environmentId = null,
        object? before = null,
        object? after = null,
        CancellationToken ct = default)
    {
        string? changes = null;
        if (before is not null || after is not null)
        {
            changes = JsonSerializer.Serialize(new
            {
                before,
                after
            }, JsonOptions);
        }

        var actorUserId = ResolveActorUserId();

        var log = new AuditLog
        {
            ResourceType = resourceType,
            ResourceId = resourceId,
            Action = action,
            OrganizationId = organizationId,
            ProjectId = projectId,
            EnvironmentId = environmentId,
            ActorUserId = actorUserId,
            Changes = changes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(ct);

        await webhookQueue.EnqueueAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.AuditLogCreated,
            EnvironmentId = environmentId,
            OrganizationId = organizationId,
            Data = new
            {
                audit_log_id = log.Id,
                resource_type = resourceType,
                resource_id = resourceId,
                action,
                created_at = log.CreatedAt
            }
        });
    }

    // Backward-compatible overload used by existing callers
    public virtual async Task LogAsync(
        string resourceType,
        string resourceId,
        string action,
        int organizationId,
        int? projectId = null,
        int? environmentId = null,
        string? changes = null,
        CancellationToken ct = default)
    {
        var actorUserId = ResolveActorUserId();

        var log = new AuditLog
        {
            ResourceType = resourceType,
            ResourceId = resourceId,
            Action = action,
            OrganizationId = organizationId,
            ProjectId = projectId,
            EnvironmentId = environmentId,
            ActorUserId = actorUserId,
            Changes = changes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(ct);

        await webhookQueue.EnqueueAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.AuditLogCreated,
            EnvironmentId = environmentId,
            OrganizationId = organizationId,
            Data = new
            {
                audit_log_id = log.Id,
                resource_type = resourceType,
                resource_id = resourceId,
                action,
                created_at = log.CreatedAt
            }
        });
    }

    private int? ResolveActorUserId()
    {
        var claim = httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
