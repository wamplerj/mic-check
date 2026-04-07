using NUnit.Framework;
using MicCheck.Api.Webhooks;

namespace MicCheck.Api.Tests.Unit.Webhooks;

[TestFixture]
public class WebhookTests
{
    [Test]
    public void WhenAWebhookIsCreated_ThenUrlAndScopeAreSet()
    {
        var webhook = new Webhook { Url = "https://example.com/hook", Scope = WebhookScope.Environment };

        Assert.That(webhook.Url, Is.EqualTo("https://example.com/hook"));
        Assert.That(webhook.Scope, Is.EqualTo(WebhookScope.Environment));
    }

    [Test]
    public void WhenAWebhookIsCreated_ThenSecretDefaultsToNull()
    {
        var webhook = new Webhook { Url = "https://example.com/hook", Scope = WebhookScope.Organization };

        Assert.That(webhook.Secret, Is.Null);
    }

    [Test]
    public void WhenAWebhookIsCreated_ThenEnabledDefaultsToFalse()
    {
        var webhook = new Webhook { Url = "https://example.com/hook", Scope = WebhookScope.Environment };

        Assert.That(webhook.Enabled, Is.False);
    }

    [Test]
    public void WhenAllWebhookScopesAreChecked_ThenAllExpectedValuesExist()
    {
        var scopes = Enum.GetValues<WebhookScope>();

        Assert.That(scopes, Contains.Item(WebhookScope.Environment));
        Assert.That(scopes, Contains.Item(WebhookScope.Organization));
    }
}

[TestFixture]
public class WebhookDeliveryLogTests
{
    [Test]
    public void WhenAWebhookDeliveryLogIsCreated_ThenRequiredFieldsAreSet()
    {
        var log = new WebhookDeliveryLog
        {
            WebhookId = 1,
            EventType = "FLAG_UPDATED",
            PayloadJson = "{}"
        };

        Assert.That(log.WebhookId, Is.EqualTo(1));
        Assert.That(log.EventType, Is.EqualTo("FLAG_UPDATED"));
        Assert.That(log.PayloadJson, Is.EqualTo("{}"));
    }

    [Test]
    public void WhenAWebhookDeliveryLogIsCreated_ThenOptionalFieldsDefaultToNull()
    {
        var log = new WebhookDeliveryLog
        {
            WebhookId = 1,
            EventType = "FLAG_UPDATED",
            PayloadJson = "{}"
        };

        Assert.That(log.ResponseStatusCode, Is.Null);
        Assert.That(log.ResponseBody, Is.Null);
        Assert.That(log.ErrorMessage, Is.Null);
    }

    [Test]
    public void WhenAWebhookDeliveryLogIsCreated_ThenSuccessDefaultsToFalse()
    {
        var log = new WebhookDeliveryLog
        {
            WebhookId = 1,
            EventType = "FLAG_UPDATED",
            PayloadJson = "{}"
        };

        Assert.That(log.Success, Is.False);
    }
}
