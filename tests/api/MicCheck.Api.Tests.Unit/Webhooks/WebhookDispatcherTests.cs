using System.Net;
using System.Security.Cryptography;
using System.Text;
using MicCheck.Api.Data;
using MicCheck.Api.Webhooks;
using Microsoft.EntityFrameworkCore;
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
        var (db, factory, capturedRequests) = SetUpDispatcher(HttpStatusCode.OK);

        var org = new MicCheck.Api.Organizations.Organization { Name = "Org", CreatedAt = DateTimeOffset.UtcNow };
        db.Organizations.Add(org);
        db.SaveChanges();

        db.Webhooks.Add(new Webhook
        {
            Url = "https://example.com/hook",
            Secret = "test-secret",
            Scope = WebhookScope.Organization,
            OrganizationId = org.Id,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();

        var dispatcher = new WebhookDispatcher(db, factory, NullLogger<WebhookDispatcher>.Instance);
        var webhookEvent = new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = org.Id,
            Data = new { test = true }
        };

        await dispatcher.DispatchAsync(webhookEvent);

        Assert.That(capturedRequests, Has.Count.EqualTo(1));
        Assert.That(capturedRequests[0].Headers.Contains("X-Flagsmith-Signature"), Is.True);
    }

    [Test]
    public async Task WhenDispatchingToWebhookWithoutSecret_ThenNoSignatureHeaderIsAdded()
    {
        var (db, factory, capturedRequests) = SetUpDispatcher(HttpStatusCode.OK);

        var org = new MicCheck.Api.Organizations.Organization { Name = "Org", CreatedAt = DateTimeOffset.UtcNow };
        db.Organizations.Add(org);
        db.SaveChanges();

        db.Webhooks.Add(new Webhook
        {
            Url = "https://example.com/hook",
            Secret = null,
            Scope = WebhookScope.Organization,
            OrganizationId = org.Id,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();

        var dispatcher = new WebhookDispatcher(db, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = org.Id,
            Data = new { }
        });

        Assert.That(capturedRequests[0].Headers.Contains("X-Flagsmith-Signature"), Is.False);
    }

    [Test]
    public async Task WhenDeliverySucceeds_ThenDeliveryLogIsRecordedAsSuccess()
    {
        var (db, factory, _) = SetUpDispatcher(HttpStatusCode.OK);

        var org = new MicCheck.Api.Organizations.Organization { Name = "Org", CreatedAt = DateTimeOffset.UtcNow };
        db.Organizations.Add(org);
        db.SaveChanges();

        db.Webhooks.Add(new Webhook
        {
            Url = "https://example.com/hook",
            Scope = WebhookScope.Organization,
            OrganizationId = org.Id,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();

        var dispatcher = new WebhookDispatcher(db, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = org.Id,
            Data = new { }
        });

        var log = db.WebhookDeliveryLogs.First();
        Assert.That(log.Success, Is.True);
        Assert.That(log.ResponseStatusCode, Is.EqualTo(200));
        Assert.That(log.AttemptNumber, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenDeliveryFails_ThenDeliveryLogIsRecordedAsFailure()
    {
        var (db, factory, _) = SetUpDispatcher(HttpStatusCode.InternalServerError);

        var org = new MicCheck.Api.Organizations.Organization { Name = "Org", CreatedAt = DateTimeOffset.UtcNow };
        db.Organizations.Add(org);
        db.SaveChanges();

        db.Webhooks.Add(new Webhook
        {
            Url = "https://example.com/hook",
            Scope = WebhookScope.Organization,
            OrganizationId = org.Id,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();

        var dispatcher = new WebhookDispatcher(db, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = org.Id,
            Data = new { }
        });

        var log = db.WebhookDeliveryLogs.First();
        Assert.That(log.Success, Is.False);
        Assert.That(log.ResponseStatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task WhenNoWebhooksAreRegistered_ThenNoDeliveryLogsAreCreated()
    {
        var (db, factory, _) = SetUpDispatcher(HttpStatusCode.OK);

        var dispatcher = new WebhookDispatcher(db, factory, NullLogger<WebhookDispatcher>.Instance);
        await dispatcher.DispatchAsync(new WebhookEvent
        {
            EventType = WebhookEventTypes.FlagUpdated,
            OrganizationId = 99,
            Data = new { }
        });

        Assert.That(db.WebhookDeliveryLogs.Count(), Is.EqualTo(0));
    }

    private static (MicCheckDbContext Db, IHttpClientFactory Factory, List<HttpRequestMessage> CapturedRequests)
        SetUpDispatcher(HttpStatusCode responseStatus)
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new MicCheckDbContext(options);

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

        return (db, factory.Object, capturedRequests);
    }
}
