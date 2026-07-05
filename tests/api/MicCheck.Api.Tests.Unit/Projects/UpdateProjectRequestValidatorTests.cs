using MicCheck.Api.Projects;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Projects;

[TestFixture]
public class UpdateProjectRequestValidatorTests
{
    private UpdateProjectRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateProjectRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new UpdateProjectRequest("Renamed", true));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenNameIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new UpdateProjectRequest("", false));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenNameExceedsMaximumLength_ThenValidationFails()
    {
        var result = _validator.Validate(new UpdateProjectRequest(new string('a', 201), false));

        Assert.That(result.IsValid, Is.False);
    }
}
