namespace MicCheck.Api.Webhooks;

public record WebhookResponse(
    int Id,
    string Url,
    string? Secret,
    string Scope,
    bool Enabled,
    int? EnvironmentId,
    int? OrganizationId,
    DateTimeOffset CreatedAt
)
{
    public static WebhookResponse From(Webhook webhook) => new(
        webhook.Id,
        webhook.Url,
        webhook.Secret,
        webhook.Scope.ToString(),
        webhook.Enabled,
        webhook.EnvironmentId,
        webhook.OrganizationId,
        webhook.CreatedAt);
}
