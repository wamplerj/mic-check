using MicCheck.Api.Features;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class CreateTagRequestValidatorTests
{
    private CreateTagRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateTagRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new CreateTagRequest("Beta", "#FF0000"));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenLabelIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateTagRequest("", "#FF0000"));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenLabelExceedsMaximumLength_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateTagRequest(new string('a', 101), "#FF0000"));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenColorIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateTagRequest("Beta", ""));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenColorIsNotAValidHexCode_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateTagRequest("Beta", "red"));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenColorIsAShortHandHexCode_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new CreateTagRequest("Beta", "#F00"));

        Assert.That(result.IsValid, Is.True);
    }
}
