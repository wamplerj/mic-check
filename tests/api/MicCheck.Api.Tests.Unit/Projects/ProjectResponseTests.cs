using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Projects;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Projects;

[TestFixture]
public class ProjectResponseTests
{
    [Test]
    public void WhenMappingAProject_ThenAllFieldsAreCopied()
    {
        var project = new Project { Id = 1, Name = "My Project", OrganizationId = 5, HideDisabledFlags = true, CreatedAt = DateTimeOffset.UtcNow };

        var response = ProjectResponse.From(project);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.Name, Is.EqualTo("My Project"));
        Assert.That(response.OrganizationId, Is.EqualTo(5));
        Assert.That(response.HideDisabledFlags, Is.True);
    }
}

[TestFixture]
public class UserPermissionResponseTests
{
    [Test]
    public void WhenMappingAUserProjectPermission_ThenPermissionsAreMappedToStrings()
    {
        var permission = new UserProjectPermission
        {
            UserId = 1,
            ProjectId = 2,
            IsAdmin = true,
            Permissions = [ProjectPermission.ViewProject, ProjectPermission.EditFeature]
        };

        var response = UserPermissionResponse.From(permission);

        Assert.That(response.UserId, Is.EqualTo(1));
        Assert.That(response.ProjectId, Is.EqualTo(2));
        Assert.That(response.IsAdmin, Is.True);
        Assert.That(response.Permissions, Is.EqualTo(new[] { "ViewProject", "EditFeature" }));
    }
}
