using MicCheck.Api.Webhooks;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Webhooks;

[TestFixture]
public class CreateWebhookRequestValidatorTests
{
    private CreateWebhookRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateWebhookRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new CreateWebhookRequest("https://example.com/hook", "secret", true));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenUrlIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateWebhookRequest("", null, true));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenUrlExceedsMaximumLength_ThenValidationFails()
    {
        var longUrl = "https://example.com/" + new string('a', 500);
        var result = _validator.Validate(new CreateWebhookRequest(longUrl, null, true));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenUrlIsNotAnAbsoluteUri_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateWebhookRequest("not a valid url", null, true));

        Assert.That(result.IsValid, Is.False);
    }
}
