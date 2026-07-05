using MicCheck.Api.Projects;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Projects;

[TestFixture]
public class CreateProjectRequestValidatorTests
{
    private CreateProjectRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateProjectRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new CreateProjectRequest("My Project", 1));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenNameIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateProjectRequest("", 1));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenNameExceedsMaximumLength_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateProjectRequest(new string('a', 201), 1));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenOrganizationIdIsZeroOrLess_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateProjectRequest("My Project", 0));

        Assert.That(result.IsValid, Is.False);
    }
}
