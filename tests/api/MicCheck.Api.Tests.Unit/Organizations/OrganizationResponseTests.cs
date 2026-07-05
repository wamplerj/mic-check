using MicCheck.Api.Organizations;
using MicCheck.Api.Users;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Organizations;

[TestFixture]
public class OrganizationResponseTests
{
    [Test]
    public void WhenMappingAnOrganizationWithoutSpecifyingPrimary_ThenIsPrimaryDefaultsToFalse()
    {
        var org = new Organization { Id = 1, Name = "My Org", CreatedAt = DateTimeOffset.UtcNow };

        var response = OrganizationResponse.From(org);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.Name, Is.EqualTo("My Org"));
        Assert.That(response.IsPrimary, Is.False);
    }

    [Test]
    public void WhenMappingAnOrganizationAsPrimary_ThenIsPrimaryIsTrue()
    {
        var org = new Organization { Id = 1, Name = "My Org", CreatedAt = DateTimeOffset.UtcNow };

        var response = OrganizationResponse.From(org, isPrimary: true);

        Assert.That(response.IsPrimary, Is.True);
    }
}

[TestFixture]
public class OrganizationMemberResponseTests
{
    [Test]
    public void WhenMappingAMember_ThenFieldsAreSourcedFromTheAssociatedUser()
    {
        var user = new User
        {
            Id = 1,
            Email = "alice@example.com",
            PasswordHash = "hashed",
            FirstName = "Alice",
            LastName = "Smith",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var member = new OrganizationUser { OrganizationId = 1, UserId = 1, Role = OrganizationRole.Admin, User = user };

        var response = OrganizationMemberResponse.From(member);

        Assert.That(response.UserId, Is.EqualTo(1));
        Assert.That(response.FirstName, Is.EqualTo("Alice"));
        Assert.That(response.LastName, Is.EqualTo("Smith"));
        Assert.That(response.Email, Is.EqualTo("alice@example.com"));
        Assert.That(response.Role, Is.EqualTo("Admin"));
    }
}
