namespace MicCheck.Api.Webhooks;

public class WebhookEvent
{
    public required string EventType { get; init; }
    public int? EnvironmentId { get; init; }
    public int OrganizationId { get; init; }
    public required object Data { get; init; }
}

public static class WebhookEventTypes
{
    public const string FlagUpdated = "FLAG_UPDATED";
    public const string FlagDeleted = "FLAG_DELETED";
    public const string AuditLogCreated = "AUDIT_LOG_CREATED";
}
