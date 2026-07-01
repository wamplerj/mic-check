using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Webhooks;

public class WebhookRetryBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookRetryBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30)];
    private const int MaxAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRetriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during webhook retry processing");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessPendingRetriesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<WebhookDispatcher>();

        var now = DateTimeOffset.UtcNow;

        for (var attemptNumber = 1; attemptNumber < MaxAttempts; attemptNumber++)
        {
            var delay = RetryDelays[attemptNumber - 1];
            var retryAfter = now - delay;

            var failedDeliveries = await db.WebhookDeliveryLogs
                .Where(d => !d.Success && d.AttemptNumber == attemptNumber && d.AttemptedAt <= retryAfter)
                .ToListAsync(ct);

            foreach (var delivery in failedDeliveries)
            {
                var alreadyRetried = await db.WebhookDeliveryLogs
                    .AnyAsync(d => d.WebhookId == delivery.WebhookId
                        && d.AttemptNumber == attemptNumber + 1
                        && d.AttemptedAt > delivery.AttemptedAt, ct);

                if (alreadyRetried) continue;

                var webhook = await db.Webhooks.FirstOrDefaultAsync(w => w.Id == delivery.WebhookId, ct);
                if (webhook is null || !webhook.Enabled) continue;

                try
                {
                    var orgId = webhook.OrganizationId ?? 0;
                    var webhookEvent = new WebhookEvent
                    {
                        EventType = delivery.EventType,
                        EnvironmentId = webhook.EnvironmentId,
                        OrganizationId = orgId,
                        Data = delivery.PayloadJson
                    };

                    await dispatcher.DispatchAsync(webhookEvent, attemptNumber + 1, ct);
                    logger.LogInformation("Retried webhook {WebhookId} attempt {Attempt}", delivery.WebhookId, attemptNumber + 1);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Retry attempt {Attempt} failed for webhook {WebhookId}", attemptNumber + 1, delivery.WebhookId);
                }
            }
        }
    }
}
