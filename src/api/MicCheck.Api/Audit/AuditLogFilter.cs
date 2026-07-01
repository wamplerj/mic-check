namespace MicCheck.Api.Audit;

public record AuditLogFilter(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? ResourceType = null,
    string? Action = null,
    int? ProjectId = null,
    int? EnvironmentId = null,
    int? ActorUserId = null,
    int Page = 1,
    int PageSize = 20
);
