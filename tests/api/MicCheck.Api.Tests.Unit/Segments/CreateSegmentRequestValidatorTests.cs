using MicCheck.Api.Segments;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Segments;

[TestFixture]
public class CreateSegmentRequestValidatorTests
{
    private CreateSegmentRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateSegmentRequestValidator();

    private static CreateSegmentRequest ValidRequest() => new(
        "Premium Users",
        [new CreateSegmentRuleRequest("All", [new CreateSegmentConditionRequest("plan", "Equal", "premium")])]);

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenTheNameIsEmpty_ThenValidationFails()
    {
        var request = ValidRequest() with { Name = "" };

        var result = _validator.Validate(request);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Name"), Is.True);
    }

    [Test]
    public void WhenTheNameExceedsMaximumLength_ThenValidationFails()
    {
        var request = ValidRequest() with { Name = new string('a', 201) };

        var result = _validator.Validate(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenRulesIsNull_ThenValidationFails()
    {
        var request = ValidRequest() with { Rules = null! };

        var result = _validator.Validate(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenARuleTypeIsInvalid_ThenValidationFails()
    {
        var request = new CreateSegmentRequest(
            "Test",
            [new CreateSegmentRuleRequest("NotAType", [new CreateSegmentConditionRequest("plan", "Equal", "premium")])]);

        var result = _validator.Validate(request);

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase("All")]
    [TestCase("Any")]
    [TestCase("None")]
    [TestCase("all")]
    public void WhenARuleTypeIsAValidCaseInsensitiveName_ThenValidationSucceeds(string type)
    {
        var request = new CreateSegmentRequest(
            "Test",
            [new CreateSegmentRuleRequest(type, [new CreateSegmentConditionRequest("plan", "Equal", "premium")])]);

        var result = _validator.Validate(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenAConditionPropertyIsEmpty_ThenValidationFails()
    {
        var request = new CreateSegmentRequest(
            "Test",
            [new CreateSegmentRuleRequest("All", [new CreateSegmentConditionRequest("", "Equal", "premium")])]);

        var result = _validator.Validate(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenAConditionOperatorIsInvalid_ThenValidationFails()
    {
        var request = new CreateSegmentRequest(
            "Test",
            [new CreateSegmentRuleRequest("All", [new CreateSegmentConditionRequest("plan", "NotAnOperator", "premium")])]);

        var result = _validator.Validate(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenANestedChildRuleHasAnInvalidOperator_ThenValidationStillSucceedsBecauseChildRulesAreNotValidatedRecursively()
    {
        var childRule = new CreateSegmentRuleRequest("All", [new CreateSegmentConditionRequest("plan", "NotAnOperator", "premium")]);
        var request = new CreateSegmentRequest(
            "Test",
            [new CreateSegmentRuleRequest("All", [new CreateSegmentConditionRequest("plan", "Equal", "premium")], [childRule])]);

        var result = _validator.Validate(request);

        Assert.That(result.IsValid, Is.True);
    }
}
