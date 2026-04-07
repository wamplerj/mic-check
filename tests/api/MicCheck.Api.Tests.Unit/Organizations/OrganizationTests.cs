using NUnit.Framework;
using MicCheck.Api.ApiKeys;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;

namespace MicCheck.Api.Tests.Unit.Organizations;

[TestFixture]
public class OrganizationTests
{
    [Test]
    public void WhenAnOrganizationIsCreated_ThenCollectionsAreInitializedEmpty()
    {
        var organization = new Organization { Name = "Acme" };

        Assert.That(organization.Projects, Is.Empty);
        Assert.That(organization.Members, Is.Empty);
        Assert.That(organization.ApiKeys, Is.Empty);
    }

    [Test]
    public void WhenAnOrganizationIsCreated_ThenNameIsSet()
    {
        var organization = new Organization { Name = "Acme" };

        Assert.That(organization.Name, Is.EqualTo("Acme"));
    }

    [Test]
    public void WhenAProjectIsAddedToAnOrganization_ThenItAppearsInTheProjectsCollection()
    {
        var organization = new Organization { Name = "Acme" };
        var project = new Project { Name = "My Project", OrganizationId = organization.Id };

        organization.Projects.Add(project);

        Assert.That(organization.Projects, Has.Count.EqualTo(1));
    }

    [Test]
    public void WhenAnApiKeyIsAddedToAnOrganization_ThenItAppearsInTheApiKeysCollection()
    {
        var organization = new Organization { Name = "Acme" };
        var apiKey = new ApiKey { Key = "hashed-key", Prefix = "abc12345", Name = "CI Key", OrganizationId = organization.Id };

        organization.ApiKeys.Add(apiKey);

        Assert.That(organization.ApiKeys, Has.Count.EqualTo(1));
    }
}

[TestFixture]
public class OrganizationUserTests
{
    [Test]
    public void WhenAnOrganizationUserIsCreated_ThenRoleDefaultsToUser()
    {
        var member = new OrganizationUser { OrganizationId = 1, UserId = 1 };

        Assert.That(member.Role, Is.EqualTo(OrganizationRole.User));
    }

    [Test]
    public void WhenAnOrganizationUserRoleIsSetToAdmin_ThenRoleIsAdmin()
    {
        var member = new OrganizationUser { OrganizationId = 1, UserId = 1, Role = OrganizationRole.Admin };

        Assert.That(member.Role, Is.EqualTo(OrganizationRole.Admin));
    }
}
