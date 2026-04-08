using MicCheck.Api.Data;

namespace MicCheck.Api.Audit;

public class AuditService(MicCheckDbContext db, IHttpContextAccessor httpContextAccessor)
{
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
        int? actorUserId = null;

        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value;
        if (int.TryParse(userIdClaim, out var parsedId))
            actorUserId = parsedId;

        db.AuditLogs.Add(new AuditLog
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
        });

        await db.SaveChangesAsync(ct);
    }
}
