using MicCheck.Api.Webhooks;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Webhooks;

[TestFixture]
public class WebhookResponseTests
{
    [Test]
    public void WhenMappingAnEnvironmentScopedWebhook_ThenScopeIsSerializedAsItsName()
    {
        var webhook = new Webhook
        {
            Id = 1,
            Url = "https://example.com/hook",
            Secret = "s3cr3t",
            Scope = WebhookScope.Environment,
            EnvironmentId = 5,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var response = WebhookResponse.From(webhook);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.Url, Is.EqualTo("https://example.com/hook"));
        Assert.That(response.Secret, Is.EqualTo("s3cr3t"));
        Assert.That(response.Scope, Is.EqualTo("Environment"));
        Assert.That(response.Enabled, Is.True);
        Assert.That(response.EnvironmentId, Is.EqualTo(5));
        Assert.That(response.OrganizationId, Is.Null);
    }

    [Test]
    public void WhenMappingAnOrganizationScopedWebhook_ThenScopeIsSerializedAsItsName()
    {
        var webhook = new Webhook
        {
            Id = 2,
            Url = "https://example.com/hook",
            Scope = WebhookScope.Organization,
            OrganizationId = 9,
            Enabled = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var response = WebhookResponse.From(webhook);

        Assert.That(response.Scope, Is.EqualTo("Organization"));
        Assert.That(response.OrganizationId, Is.EqualTo(9));
        Assert.That(response.EnvironmentId, Is.Null);
    }
}

[TestFixture]
public class WebhookDeliveryLogResponseTests
{
    [Test]
    public void WhenMappingASuccessfulDelivery_ThenAllFieldsAreCopied()
    {
        var log = new WebhookDeliveryLog
        {
            Id = 1,
            WebhookId = 2,
            EventType = "FLAG_UPDATED",
            PayloadJson = "{}",
            ResponseStatusCode = 200,
            ResponseBody = "ok",
            Success = true,
            AttemptNumber = 1,
            AttemptedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(42)
        };

        var response = WebhookDeliveryLogResponse.From(log);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.WebhookId, Is.EqualTo(2));
        Assert.That(response.EventType, Is.EqualTo("FLAG_UPDATED"));
        Assert.That(response.Success, Is.True);
        Assert.That(response.ResponseStatusCode, Is.EqualTo(200));
        Assert.That(response.ResponseBody, Is.EqualTo("ok"));
        Assert.That(response.ErrorMessage, Is.Null);
        Assert.That(response.AttemptNumber, Is.EqualTo(1));
        Assert.That(response.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(42)));
    }

    [Test]
    public void WhenMappingAFailedDelivery_ThenErrorMessageIsCopied()
    {
        var log = new WebhookDeliveryLog
        {
            Id = 1,
            WebhookId = 2,
            EventType = "FLAG_UPDATED",
            PayloadJson = "{}",
            Success = false,
            ErrorMessage = "Connection refused",
            AttemptNumber = 2,
            AttemptedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero
        };

        var response = WebhookDeliveryLogResponse.From(log);

        Assert.That(response.Success, Is.False);
        Assert.That(response.ErrorMessage, Is.EqualTo("Connection refused"));
        Assert.That(response.ResponseStatusCode, Is.Null);
    }
}
