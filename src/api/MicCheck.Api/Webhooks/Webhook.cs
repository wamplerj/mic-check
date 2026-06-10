namespace MicCheck.Api.Webhooks;

public class Webhook
{
    public int Id { get; init; }
    public required string Url { get; set; }
    public string? Secret { get; set; }
    public WebhookScope Scope { get; set; }
    public int? EnvironmentId { get; set; }
    public int? OrganizationId { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}

public enum WebhookScope { Environment, Organization }
