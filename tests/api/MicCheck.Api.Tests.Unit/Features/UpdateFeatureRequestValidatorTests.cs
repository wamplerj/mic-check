using MicCheck.Api.Features;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class UpdateFeatureRequestValidatorTests
{
    private UpdateFeatureRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateFeatureRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new UpdateFeatureRequest("dark_mode", "A description"));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenNameIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new UpdateFeatureRequest("", null));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenNameExceedsMaximumLength_ThenValidationFails()
    {
        var result = _validator.Validate(new UpdateFeatureRequest(new string('a', 151), null));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenNameContainsInvalidCharacters_ThenValidationFails()
    {
        var result = _validator.Validate(new UpdateFeatureRequest("dark mode!", null));

        Assert.That(result.IsValid, Is.False);
    }
}
