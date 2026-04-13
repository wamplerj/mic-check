using System.Threading.Channels;

namespace MicCheck.Api.Webhooks;

public class WebhookQueue
{
    private readonly Channel<WebhookEvent> _channel = Channel.CreateUnbounded<WebhookEvent>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(WebhookEvent webhookEvent) =>
        _channel.Writer.WriteAsync(webhookEvent);

    public IAsyncEnumerable<WebhookEvent> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
