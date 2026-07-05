using System.Net;
using System.Security.Cryptography;
using System.Text;
using MicCheck.Api.Data;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Webhooks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Webhooks;

[TestFixture]
public class WebhookDispatcherTests
{
    [Test]
    public void WhenComputingSignature_ThenItMatchesHmacSha256()
    {
        const string secret = "my-secret";
        const string payload = """{"event_type":"FLAG_UPDATED","data":{}}""";

        var result = WebhookDispatcher.ComputeSignature(secret, payload);

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var expected = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(keyBytes, payloadBytes)).ToLowerInvariant();

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void WhenComputingSignatureWithDifferentPayloads_ThenResultsDiffer()
    {
        const string secret = "my-secret";
        var sig1 = WebhookDispatcher.ComputeSignature(secret, "payload-one");
        var sig2 = WebhookDispatcher.ComputeSignature(secret, "payload-two");

        Assert.That(sig1, Is.Not.EqualTo(sig2));
    }

    [Test]
    public void WhenComputingSignatureWithDifferentSecrets_ThenResultsDiffer()
    {
        const string payload = "same-payload";
        var sig1 = WebhookDispatcher.ComputeSignature("secret-a", payload);
        var sig2 = WebhookDispatcher.ComputeSignature("secret-b", payload);

        Assert.That(sig1, Is.Not.EqualTo(sig2));
    }

    [Test]
    public async Task WhenDispatchingToActiveWebhook_ThenPayloadIsPostedWithSignatureHeader()
    {
        var (db, webhooks, _, factory, capturedRequests) = SetUpDispatcher(HttpStatusCode.OK);

        const int orgId = 1;
        webhooks.Add(new Webhook
        {
            Url = "https://example.com/hook",
            Secret = "test-secret",
            Scope = WebhookScope.Organization,
            OrganizationId = orgId,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var dispatcher = new WebhookDispatcher(db.Object, factory, NullLogger<WebhookDispatcher>.Instance);
        var webhookEvent = new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = orgId,
            Data = new { test = true }
        };

        await dispatcher.DispatchAsync(webhookEvent);

        Assert.That(capturedRequests, Has.Count.EqualTo(1));
        Assert.That(capturedRequests[0].Headers.Contains("X-Flagsmith-Signature"), Is.True);
    }

    [Test]
    public async Task WhenDispatchingToWebhookWithoutSecret_ThenNoSignatureHeaderIsAdded()
    {
        var (db, webhooks, _, factory, capturedRequests) = SetUpDispatcher(HttpStatusCode.OK);

        const int orgId = 1;
        webhooks.Add(new Webhook
        {
            Url = "https://example.com/hook",
            Secret = null,
            Scope = WebhookScope.Organization,
            OrganizationId = orgId,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var dispatcher = new WebhookDispatcher(db.Object, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = orgId,
            Data = new { }
        });

        Assert.That(capturedRequests[0].Headers.Contains("X-Flagsmith-Signature"), Is.False);
    }

    [Test]
    public async Task WhenDeliverySucceeds_ThenDeliveryLogIsRecordedAsSuccess()
    {
        var (db, webhooks, deliveryLogs, factory, _) = SetUpDispatcher(HttpStatusCode.OK);

        const int orgId = 1;
        webhooks.Add(new Webhook
        {
            Url = "https://example.com/hook",
            Scope = WebhookScope.Organization,
            OrganizationId = orgId,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var dispatcher = new WebhookDispatcher(db.Object, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = orgId,
            Data = new { }
        });

        var log = deliveryLogs.First();
        Assert.That(log.Success, Is.True);
        Assert.That(log.ResponseStatusCode, Is.EqualTo(200));
        Assert.That(log.AttemptNumber, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenDeliveryFails_ThenDeliveryLogIsRecordedAsFailure()
    {
        var (db, webhooks, deliveryLogs, factory, _) = SetUpDispatcher(HttpStatusCode.InternalServerError);

        const int orgId = 1;
        webhooks.Add(new Webhook
        {
            Url = "https://example.com/hook",
            Scope = WebhookScope.Organization,
            OrganizationId = orgId,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var dispatcher = new WebhookDispatcher(db.Object, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = orgId,
            Data = new { }
        });

        var log = deliveryLogs.First();
        Assert.That(log.Success, Is.False);
        Assert.That(log.ResponseStatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task WhenAnEnvironmentScopedEventIsDispatched_ThenOrganizationScopedWebhooksForThatOrgAlsoReceiveIt()
    {
        var (db, webhooks, deliveryLogs, factory, _) = SetUpDispatcher(HttpStatusCode.OK);

        const int orgId = 1;
        const int envId = 5;
        webhooks.Add(new Webhook { Url = "https://env.example.com", Scope = WebhookScope.Environment, EnvironmentId = envId, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });
        webhooks.Add(new Webhook { Url = "https://org.example.com", Scope = WebhookScope.Organization, OrganizationId = orgId, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });
        webhooks.Add(new Webhook { Url = "https://other-env.example.com", Scope = WebhookScope.Environment, EnvironmentId = 999, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });

        var dispatcher = new WebhookDispatcher(db.Object, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            EnvironmentId = envId,
            OrganizationId = orgId,
            Data = new { }
        });

        Assert.That(deliveryLogs, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task WhenAnOrganizationScopedEventIsDispatched_ThenEnvironmentScopedWebhooksAreNotIncluded()
    {
        var (db, webhooks, deliveryLogs, factory, _) = SetUpDispatcher(HttpStatusCode.OK);

        const int orgId = 1;
        webhooks.Add(new Webhook { Url = "https://env.example.com", Scope = WebhookScope.Environment, EnvironmentId = 5, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });
        webhooks.Add(new Webhook { Url = "https://org.example.com", Scope = WebhookScope.Organization, OrganizationId = orgId, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });

        var dispatcher = new WebhookDispatcher(db.Object, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.AuditLogCreated,
            EnvironmentId = null,
            OrganizationId = orgId,
            Data = new { }
        });

        Assert.That(deliveryLogs, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenAWebhookIsDisabled_ThenItIsExcludedFromDispatch()
    {
        var (db, webhooks, deliveryLogs, factory, _) = SetUpDispatcher(HttpStatusCode.OK);

        const int orgId = 1;
        webhooks.Add(new Webhook { Url = "https://example.com", Scope = WebhookScope.Organization, OrganizationId = orgId, Enabled = false, CreatedAt = DateTimeOffset.UtcNow });

        var dispatcher = new WebhookDispatcher(db.Object, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = orgId,
            Data = new { }
        });

        Assert.That(deliveryLogs, Is.Empty);
    }

    [Test]
    public async Task WhenTheHttpClientThrows_ThenTheDeliveryLogRecordsTheErrorMessage()
    {
        var db = new Mock<IMicCheckDbContext>();
        var webhooks = new List<Webhook> { new() { Url = "https://example.com", Scope = WebhookScope.Organization, OrganizationId = 1, Enabled = true, CreatedAt = DateTimeOffset.UtcNow } };
        db.SetupDbSetWithGeneratedIds(c => c.Webhooks, webhooks);
        var deliveryLogs = new List<WebhookDeliveryLog>();
        db.SetupDbSet(c => c.WebhookDeliveryLogs, deliveryLogs);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var dispatcher = new WebhookDispatcher(db.Object, factory.Object, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent { EventType = WebhookEventTypes.FlagUpdated, OrganizationId = 1, Data = new { } });

        var log = deliveryLogs.First();
        Assert.That(log.Success, Is.False);
        Assert.That(log.ErrorMessage, Is.EqualTo("Connection refused"));
        Assert.That(log.ResponseStatusCode, Is.Null);
    }

    [Test]
    public async Task WhenNoWebhooksAreRegistered_ThenNoDeliveryLogsAreCreated()
    {
        var (db, _, deliveryLogs, factory, _) = SetUpDispatcher(HttpStatusCode.OK);

        var dispatcher = new WebhookDispatcher(db.Object, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = 99,
            Data = new { }
        });

        Assert.That(deliveryLogs, Is.Empty);
    }

    private static (Mock<IMicCheckDbContext> Db, List<Webhook> Webhooks, List<WebhookDeliveryLog> DeliveryLogs, IHttpClientFactory Factory, List<HttpRequestMessage> CapturedRequests)
        SetUpDispatcher(HttpStatusCode responseStatus)
    {
        var db = new Mock<IMicCheckDbContext>();
        var webhooks = new List<Webhook>();
        db.SetupDbSetWithGeneratedIds(c => c.Webhooks, webhooks);
        var deliveryLogs = new List<WebhookDeliveryLog>();
        db.SetupDbSet(c => c.WebhookDeliveryLogs, deliveryLogs);

        var capturedRequests = new List<HttpRequestMessage>();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                capturedRequests.Add(req);
                return new HttpResponseMessage(responseStatus)
                {
                    Content = new StringContent("ok")
                };
            });

        var httpClient = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return (db, webhooks, deliveryLogs, factory.Object, capturedRequests);
    }
}
