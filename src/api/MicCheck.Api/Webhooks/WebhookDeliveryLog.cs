namespace MicCheck.Api.Webhooks;

public class WebhookDeliveryLog
{
    public int Id { get; init; }
    public int WebhookId { get; init; }
    public required string EventType { get; init; }
    public required string PayloadJson { get; init; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset AttemptedAt { get; init; }
    public TimeSpan Duration { get; set; }
}
