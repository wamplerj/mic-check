using MicCheck.Api.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MicCheck.Api.Webhooks;

public class WebhookBackgroundService(
    WebhookQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var webhookEvent in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<WebhookDispatcher>();
                await dispatcher.DispatchAsync(webhookEvent, attemptNumber: 1, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error dispatching webhook event {EventType}", webhookEvent.EventType);
            }
        }
    }
}
