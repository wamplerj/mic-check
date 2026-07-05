using MicCheck.Api.Environments;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Environments;

[TestFixture]
public class CreateEnvironmentRequestValidatorTests
{
    private CreateEnvironmentRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateEnvironmentRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new CreateEnvironmentRequest("Staging", 1));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenNameIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateEnvironmentRequest("", 1));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenNameExceedsMaximumLength_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateEnvironmentRequest(new string('a', 201), 1));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenProjectIdIsZeroOrLess_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateEnvironmentRequest("Staging", 0));

        Assert.That(result.IsValid, Is.False);
    }
}

[TestFixture]
public class UpdateEnvironmentRequestValidatorTests
{
    private UpdateEnvironmentRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateEnvironmentRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new UpdateEnvironmentRequest("Renamed"));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenNameIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new UpdateEnvironmentRequest(""));

        Assert.That(result.IsValid, Is.False);
    }
}

[TestFixture]
public class CloneEnvironmentRequestValidatorTests
{
    private CloneEnvironmentRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CloneEnvironmentRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new CloneEnvironmentRequest("Staging Copy"));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenNameIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new CloneEnvironmentRequest(""));

        Assert.That(result.IsValid, Is.False);
    }
}
