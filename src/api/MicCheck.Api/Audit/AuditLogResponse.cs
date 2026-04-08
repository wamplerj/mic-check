namespace MicCheck.Api.Audit;

public record AuditLogResponse(
    int Id,
    string ResourceType,
    string ResourceId,
    string Action,
    string? Changes,
    int OrganizationId,
    int? ProjectId,
    int? EnvironmentId,
    int? ActorUserId,
    DateTimeOffset CreatedAt
)
{
    public static AuditLogResponse From(AuditLog log) => new(
        log.Id,
        log.ResourceType,
        log.ResourceId,
        log.Action,
        log.Changes,
        log.OrganizationId,
        log.ProjectId,
        log.EnvironmentId,
        log.ActorUserId,
        log.CreatedAt);
}
