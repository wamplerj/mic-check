using MicCheck.Api.Features;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class CreateFeatureRequestValidatorTests
{
    private CreateFeatureRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateFeatureRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new CreateFeatureRequest("dark_mode", FeatureType.Standard, null, null));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenNameIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateFeatureRequest("", FeatureType.Standard, null, null));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenNameExceedsMaximumLength_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateFeatureRequest(new string('a', 151), FeatureType.Standard, null, null));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenNameContainsInvalidCharacters_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateFeatureRequest("dark mode!", FeatureType.Standard, null, null));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenInitialValueExceedsMaximumLength_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateFeatureRequest("flag", FeatureType.Standard, new string('a', 20_001), null));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenInitialValueIsNull_ThenTheLengthRuleIsSkipped()
    {
        var result = _validator.Validate(new CreateFeatureRequest("flag", FeatureType.Standard, null, null));

        Assert.That(result.IsValid, Is.True);
    }
}
