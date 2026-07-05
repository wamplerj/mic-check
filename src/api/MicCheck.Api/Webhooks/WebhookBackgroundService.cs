using System.Diagnostics.CodeAnalysis;
using MicCheck.Api.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MicCheck.Api.Webhooks;

[ExcludeFromCodeCoverage(Justification = "Long-running BackgroundService reading from a Channel and issuing DI-scoped dispatches; exercising it cleanly requires a live DI container and DB, which CLAUDE.md disallows (no WebApplicationFactory/InMemory). Dispatch logic is covered by WebhookDispatcherTests.")]
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
