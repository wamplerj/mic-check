using MicCheck.Api.Projects;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Projects;

[TestFixture]
public class SetUserPermissionsRequestValidatorTests
{
    private SetUserPermissionsRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new SetUserPermissionsRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new SetUserPermissionsRequest(1, true, ["ViewProject", "EditFeature"]));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenUserIdIsZeroOrLess_ThenValidationFails()
    {
        var result = _validator.Validate(new SetUserPermissionsRequest(0, true, []));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenAPermissionIsInvalid_ThenValidationFails()
    {
        var result = _validator.Validate(new SetUserPermissionsRequest(1, false, ["NotAPermission"]));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenPermissionsListIsEmpty_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new SetUserPermissionsRequest(1, true, []));

        Assert.That(result.IsValid, Is.True);
    }
}
