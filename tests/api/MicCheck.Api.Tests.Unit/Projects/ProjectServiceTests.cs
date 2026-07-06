using MicCheck.Api.Audit;
using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Data;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Tests.Unit.TestSupport;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Projects;

[TestFixture]
public class ProjectServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Project> _projects = null!;
    private List<UserProjectPermission> _permissions = null!;
    private ProjectService _service = null!;
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
        var auditService = new Mock<IAuditService>();
        auditService.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new ProjectService(_db.Object, auditService.Object);
    }

    [Test]
    public async Task WhenCreatingAProject_ThenItIsPersistedWithTheGivenOrganization()
    {
        var project = await _service.CreateAsync(OrganizationId, "My Project");

        Assert.That(project.Name, Is.EqualTo("My Project"));
        Assert.That(project.OrganizationId, Is.EqualTo(OrganizationId));
        Assert.That(_projects, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenListingProjectsForAnOrganization_ThenOnlyThatOrganizationsProjectsAreReturned()
    {
        await _service.CreateAsync(OrganizationId, "Project A");
        await _service.CreateAsync(999, "Other Org Project");

        var projects = await _service.ListByOrganizationAsync(OrganizationId);

        Assert.That(projects, Has.Count.EqualTo(1));
        Assert.That(projects[0].Name, Is.EqualTo("Project A"));
    }

    [Test]
    public async Task WhenFindingAProjectByIdThatExists_ThenItIsReturned()
    {
        var project = await _service.CreateAsync(OrganizationId, "My Project");

        var found = await _service.FindByIdAsync(project.Id);

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Id, Is.EqualTo(project.Id));
    }

    [Test]
    public async Task WhenFindingAProjectByIdThatDoesNotExist_ThenNullIsReturned()
    {
        var found = await _service.FindByIdAsync(999);

        Assert.That(found, Is.Null);
    }

    [Test]
    public void WhenUpdatingAProjectThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(999, "renamed", true));
    }

    [Test]
    public async Task WhenUpdatingAProject_ThenNameAndHideDisabledFlagsAreChanged()
    {
        var project = await _service.CreateAsync(OrganizationId, "Old Name");

        var updated = await _service.UpdateAsync(project.Id, "New Name", true);

        Assert.That(updated.Name, Is.EqualTo("New Name"));
        Assert.That(updated.HideDisabledFlags, Is.True);
    }

    [Test]
    public void WhenDeletingAProjectThatDoesNotExist_ThenNoExceptionIsThrown()
    {
        Assert.DoesNotThrowAsync(() => _service.DeleteAsync(999));
    }

    [Test]
    public async Task WhenDeletingAProject_ThenItIsRemovedFromTheDatabase()
    {
        var project = await _service.CreateAsync(OrganizationId, "My Project");

        await _service.DeleteAsync(project.Id);

        Assert.That(_projects.Any(p => p.Id == project.Id), Is.False);
    }

    [Test]
    public async Task WhenListingUserPermissionsForAProject_ThenOnlyThatProjectsPermissionsAreReturned()
    {
        _permissions.Add(new UserProjectPermission { ProjectId = 1, UserId = 1 });
        _permissions.Add(new UserProjectPermission { ProjectId = 999, UserId = 2 });

        var perms = await _service.ListUserPermissionsAsync(1);

        Assert.That(perms, Has.Count.EqualTo(1));
        Assert.That(perms[0].UserId, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenSettingUserPermissionsForAUserWithNoExistingPermissions_ThenANewRecordIsCreated()
    {
        var perm = await _service.SetUserPermissionsAsync(1, userId: 5, isAdmin: true, [ProjectPermission.ViewProject]);

        Assert.That(perm.IsAdmin, Is.True);
        Assert.That(perm.Permissions, Does.Contain(ProjectPermission.ViewProject));
        Assert.That(_permissions, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenSettingUserPermissionsForAUserThatAlreadyHasPermissions_ThenTheExistingRecordIsUpdated()
    {
        await _service.SetUserPermissionsAsync(1, userId: 5, isAdmin: false, [ProjectPermission.ViewProject]);

        var updated = await _service.SetUserPermissionsAsync(1, userId: 5, isAdmin: true, [ProjectPermission.DeleteFeature]);

        Assert.That(_permissions, Has.Count.EqualTo(1));
        Assert.That(updated.IsAdmin, Is.True);
        Assert.That(updated.Permissions, Is.EqualTo(new[] { ProjectPermission.DeleteFeature }));
    }

    [Test]
    public void WhenRemovingUserPermissionsThatDoNotExist_ThenNoExceptionIsThrown()
    {
        Assert.DoesNotThrowAsync(() => _service.RemoveUserPermissionsAsync(1, 5));
    }

    [Test]
    public async Task WhenRemovingUserPermissions_ThenTheRecordIsRemoved()
    {
        await _service.SetUserPermissionsAsync(1, userId: 5, isAdmin: true, [ProjectPermission.ViewProject]);

        await _service.RemoveUserPermissionsAsync(1, 5);

        Assert.That(_permissions, Is.Empty);
    }
}
