using MicCheck.Api.Webhooks;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Webhooks;

[TestFixture]
public class WebhookQueueTests
{
    [Test]
    public async Task WhenAnEventIsEnqueued_ThenItCanBeReadBack()
    {
        var queue = new WebhookQueue();
        var webhookEvent = new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = 1,
            Data = new { }
        };

        await queue.EnqueueAsync(webhookEvent);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        await enumerator.MoveNextAsync();

        Assert.That(enumerator.Current, Is.SameAs(webhookEvent));
    }

    [Test]
    public async Task WhenMultipleEventsAreEnqueued_ThenTheyAreReadInOrder()
    {
        var queue = new WebhookQueue();
        var first = new WebhookEvent { EventType = WebhookEventTypes.FlagUpdated, OrganizationId = 1, Data = new { } };
        var second = new WebhookEvent { EventType = WebhookEventTypes.FlagDeleted, OrganizationId = 1, Data = new { } };

        await queue.EnqueueAsync(first);
        await queue.EnqueueAsync(second);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        await enumerator.MoveNextAsync();
        Assert.That(enumerator.Current, Is.SameAs(first));

        await enumerator.MoveNextAsync();
        Assert.That(enumerator.Current, Is.SameAs(second));
    }
}
