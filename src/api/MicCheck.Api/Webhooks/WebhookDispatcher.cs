using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Webhooks;

public class WebhookDispatcher(MicCheckDbContext db, IHttpClientFactory httpClientFactory, ILogger<WebhookDispatcher> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task DispatchAsync(WebhookEvent webhookEvent, int attemptNumber = 1, CancellationToken ct = default)
    {
        var webhooks = await LoadWebhooksForEventAsync(webhookEvent, ct);
        if (webhooks.Count == 0) return;

        var payload = new WebhookPayload(webhookEvent.EventType, webhookEvent.Data);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        var tasks = webhooks.Select(w => DeliverAsync(w, payloadJson, attemptNumber, ct));
        await Task.WhenAll(tasks);
    }

    private async Task<IReadOnlyList<Webhook>> LoadWebhooksForEventAsync(WebhookEvent webhookEvent, CancellationToken ct)
    {
        var query = db.Webhooks.Where(w => w.Enabled);

        if (webhookEvent.EnvironmentId.HasValue)
        {
            query = query.Where(w =>
                (w.Scope == WebhookScope.Environment && w.EnvironmentId == webhookEvent.EnvironmentId) ||
                (w.Scope == WebhookScope.Organization && w.OrganizationId == webhookEvent.OrganizationId));
        }
        else
        {
            query = query.Where(w =>
                w.Scope == WebhookScope.Organization && w.OrganizationId == webhookEvent.OrganizationId);
        }

        return await query.ToListAsync(ct);
    }

    private async Task DeliverAsync(Webhook webhook, string payloadJson, int attemptNumber, CancellationToken ct)
    {
        var started = DateTimeOffset.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        int? statusCode = null;
        string? responseBody = null;
        string? errorMessage = null;
        var success = false;

        try
        {
            var client = httpClientFactory.CreateClient("Webhooks");
            using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(webhook.Secret))
                request.Headers.Add("X-Flagsmith-Signature", ComputeSignature(webhook.Secret, payloadJson));

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            using var response = await client.SendAsync(request, cts.Token);
            statusCode = (int)response.StatusCode;
            responseBody = await response.Content.ReadAsStringAsync(ct);
            success = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            logger.LogWarning(ex, "Webhook delivery failed for webhook {WebhookId} attempt {Attempt}", webhook.Id, attemptNumber);
        }
        finally
        {
            stopwatch.Stop();
        }

        db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
        {
            WebhookId = webhook.Id,
            EventType = payloadJson.Length > 100
                ? payloadJson[..100]
                : payloadJson,
            PayloadJson = payloadJson,
            ResponseStatusCode = statusCode,
            ResponseBody = responseBody,
            Success = success,
            ErrorMessage = errorMessage,
            AttemptNumber = attemptNumber,
            AttemptedAt = started,
            Duration = stopwatch.Elapsed
        });

        await db.SaveChangesAsync(ct);
    }

    public static string ComputeSignature(string secret, string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hashBytes = HMACSHA256.HashData(keyBytes, payloadBytes);
        return "sha256=" + Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
