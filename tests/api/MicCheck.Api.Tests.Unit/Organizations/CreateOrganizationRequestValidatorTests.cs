using MicCheck.Api.Organizations;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Organizations;

[TestFixture]
public class CreateOrganizationRequestValidatorTests
{
    private CreateOrganizationRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateOrganizationRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new CreateOrganizationRequest("My Org"));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenNameIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateOrganizationRequest(""));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenNameExceedsMaximumLength_ThenValidationFails()
    {
        var result = _validator.Validate(new CreateOrganizationRequest(new string('a', 201)));

        Assert.That(result.IsValid, Is.False);
    }
}

[TestFixture]
public class UpdateOrganizationRequestValidatorTests
{
    private UpdateOrganizationRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateOrganizationRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new UpdateOrganizationRequest("Renamed"));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenNameIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new UpdateOrganizationRequest(""));

        Assert.That(result.IsValid, Is.False);
    }
}

[TestFixture]
public class InviteUserRequestValidatorTests
{
    private InviteUserRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new InviteUserRequestValidator();

    [Test]
    public void WhenTheRequestIsValid_ThenValidationSucceeds()
    {
        var result = _validator.Validate(new InviteUserRequest(1, "Admin"));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void WhenUserIdIsZeroOrLess_ThenValidationFails()
    {
        var result = _validator.Validate(new InviteUserRequest(0, "User"));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenRoleIsEmpty_ThenValidationFails()
    {
        var result = _validator.Validate(new InviteUserRequest(1, ""));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void WhenRoleIsNotAValidOrganizationRole_ThenValidationFails()
    {
        var result = _validator.Validate(new InviteUserRequest(1, "SuperAdmin"));

        Assert.That(result.IsValid, Is.False);
    }
}
