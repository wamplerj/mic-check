using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Data;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Projects;

[TestFixture]
public class ProjectsControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Project> _projects = null!;
    private List<UserProjectPermission> _permissions = null!;
    private ProjectsController _controller = null!;
    private const int OrganizationId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _db.SetupDbSet(c => c.Organizations, [
            new Organization { Id = OrganizationId, Name = "Test Org", CreatedAt = DateTimeOffset.UtcNow }
        ]);
        _projects = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Projects, _projects);
        _permissions = [];
        _db.SetupDbSet(c => c.UserProjectPermissions, _permissions);

        var webhookQueue = new MicCheck.Api.Webhooks.WebhookQueue();
        var auditService = new Mock<AuditService>(_db.Object, null!, webhookQueue);
        auditService.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _controller = new ProjectsController(new ProjectService(_db.Object, auditService.Object));
    }

    [Test]
    public async Task WhenListingProjectsForAnOrganization_ThenTheyAreReturnedInAPage()
    {
        await _controller.Create(new CreateProjectRequest("Project A", OrganizationId), CancellationToken.None);
        await _controller.Create(new CreateProjectRequest("Project B", OrganizationId), CancellationToken.None);

        var result = await _controller.List(OrganizationId);

        var ok = result.Result as OkObjectResult;
        var page = (PaginatedResponse<ProjectResponse>)ok!.Value!;
        Assert.That(page.Count, Is.EqualTo(2));
        Assert.That(page.Results, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task WhenListingProjectsWithAPageSizeAboveTheMaximum_ThenItIsClampedTo100()
    {
        for (var i = 0; i < 3; i++)
            await _controller.Create(new CreateProjectRequest($"Project {i}", OrganizationId), CancellationToken.None);

        var result = await _controller.List(OrganizationId, page: 1, pageSize: 1000);

        var ok = result.Result as OkObjectResult;
        var page = (PaginatedResponse<ProjectResponse>)ok!.Value!;
        Assert.That(page.Results, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task WhenCreatingAProject_ThenACreatedResultWithTheProjectIsReturned()
    {
        var result = await _controller.Create(new CreateProjectRequest("My Project", OrganizationId), CancellationToken.None);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(((ProjectResponse)created!.Value!).Name, Is.EqualTo("My Project"));
    }

    [Test]
    public async Task WhenGettingAProjectByIdThatExists_ThenItIsReturned()
    {
        var created = await _controller.Create(new CreateProjectRequest("My Project", OrganizationId), CancellationToken.None);
        var projectId = ((ProjectResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.GetById(projectId, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(((ProjectResponse)ok!.Value!).Id, Is.EqualTo(projectId));
    }

    [Test]
    public async Task WhenGettingAProjectByIdThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetById(999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAProjectThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Update(999, new UpdateProjectRequest("renamed", false), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAProject_ThenTheUpdatedProjectIsReturned()
    {
        var created = await _controller.Create(new CreateProjectRequest("Old Name", OrganizationId), CancellationToken.None);
        var projectId = ((ProjectResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Update(projectId, new UpdateProjectRequest("New Name", true), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var response = (ProjectResponse)ok!.Value!;
        Assert.That(response.Name, Is.EqualTo("New Name"));
        Assert.That(response.HideDisabledFlags, Is.True);
    }

    [Test]
    public async Task WhenDeletingAProjectThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Delete(999, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAProject_ThenNoContentIsReturnedAndItIsRemoved()
    {
        var created = await _controller.Create(new CreateProjectRequest("My Project", OrganizationId), CancellationToken.None);
        var projectId = ((ProjectResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Delete(projectId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_projects.Any(p => p.Id == projectId), Is.False);
    }

    [Test]
    public async Task WhenListingUserPermissionsForAProjectThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListUserPermissions(999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingUserPermissionsForAProject_ThenTheyAreReturned()
    {
        var created = await _controller.Create(new CreateProjectRequest("My Project", OrganizationId), CancellationToken.None);
        var projectId = ((ProjectResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        await _controller.CreateUserPermissions(projectId, new SetUserPermissionsRequest(5, true, ["ViewProject"]), CancellationToken.None);

        var result = await _controller.ListUserPermissions(projectId, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var perms = (IReadOnlyList<UserPermissionResponse>)ok!.Value!;
        Assert.That(perms, Has.Count.EqualTo(1));
        Assert.That(perms[0].UserId, Is.EqualTo(5));
    }

    [Test]
    public async Task WhenCreatingUserPermissionsForAProjectThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.CreateUserPermissions(999, new SetUserPermissionsRequest(5, true, []), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenCreatingUserPermissions_ThenThePermissionsAreParsedAndPersisted()
    {
        var created = await _controller.Create(new CreateProjectRequest("My Project", OrganizationId), CancellationToken.None);
        var projectId = ((ProjectResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.CreateUserPermissions(
            projectId, new SetUserPermissionsRequest(5, true, ["ViewProject", "EditFeature"]), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var response = (UserPermissionResponse)ok!.Value!;
        Assert.That(response.Permissions, Is.EqualTo(new[] { "ViewProject", "EditFeature" }));
    }

    [Test]
    public async Task WhenUpdatingUserPermissionsForAProjectThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.UpdateUserPermissions(999, 5, new SetUserPermissionsRequest(5, true, []), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingUserPermissions_ThenTheExistingRecordIsChanged()
    {
        var created = await _controller.Create(new CreateProjectRequest("My Project", OrganizationId), CancellationToken.None);
        var projectId = ((ProjectResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        await _controller.CreateUserPermissions(projectId, new SetUserPermissionsRequest(5, false, ["ViewProject"]), CancellationToken.None);

        var result = await _controller.UpdateUserPermissions(projectId, 5, new SetUserPermissionsRequest(5, true, ["DeleteFeature"]), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var response = (UserPermissionResponse)ok!.Value!;
        Assert.That(response.IsAdmin, Is.True);
        Assert.That(response.Permissions, Is.EqualTo(new[] { "DeleteFeature" }));
        Assert.That(_permissions, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenDeletingUserPermissionsForAProjectThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.DeleteUserPermissions(999, 5, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingUserPermissions_ThenTheRecordIsRemoved()
    {
        var created = await _controller.Create(new CreateProjectRequest("My Project", OrganizationId), CancellationToken.None);
        var projectId = ((ProjectResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        await _controller.CreateUserPermissions(projectId, new SetUserPermissionsRequest(5, true, ["ViewProject"]), CancellationToken.None);

        var result = await _controller.DeleteUserPermissions(projectId, 5, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_permissions, Is.Empty);
    }
}
