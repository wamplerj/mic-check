namespace MicCheck.Api.Webhooks;

public record WebhookDeliveryLogResponse(
    int Id,
    int WebhookId,
    string EventType,
    bool Success,
    int? ResponseStatusCode,
    string? ResponseBody,
    string? ErrorMessage,
    int AttemptNumber,
    DateTimeOffset AttemptedAt,
    TimeSpan Duration
)
{
    public static WebhookDeliveryLogResponse From(WebhookDeliveryLog log) => new(
        log.Id,
        log.WebhookId,
        log.EventType,
        log.Success,
        log.ResponseStatusCode,
        log.ResponseBody,
        log.ErrorMessage,
        log.AttemptNumber,
        log.AttemptedAt,
        log.Duration);
}
