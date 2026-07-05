using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using MicCheck.Api.Organizations;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Users;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Organizations;

[TestFixture]
public class OrganizationServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Organization> _organizations = null!;
    private List<OrganizationUser> _members = null!;
    private List<User> _users = null!;
    private OrganizationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _organizations = [];
        _members = [];
        var organizationsSet = MockDbSetFactory.Create(_organizations);
        organizationsSet.Setup(m => m.Add(It.IsAny<Organization>())).Callback<Organization>(org =>
        {
            typeof(Organization).GetProperty(nameof(Organization.Id))!.SetValue(org, _organizations.Count + 1);
            _organizations.Add(org);
            // Mimics EF Core's cascade-insert and FK fixup of a loaded, mapped collection navigation on SaveChanges.
            foreach (var member in org.Members)
            {
                typeof(OrganizationUser).GetProperty(nameof(OrganizationUser.OrganizationId))!.SetValue(member, org.Id);
                _members.Add(member);
            }
        });
        _db.Setup(c => c.Organizations).Returns(organizationsSet.Object);
        _db.SetupDbSet(c => c.OrganizationUsers, _members);
        _users = [];
        _db.SetupDbSet(c => c.Users, _users);

        var webhookQueue = new MicCheck.Api.Webhooks.WebhookQueue();
        var auditService = new Mock<AuditService>(_db.Object, null!, webhookQueue);
        auditService.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new OrganizationService(_db.Object, auditService.Object);
    }

    [Test]
    public async Task WhenCreatingAnOrganization_ThenItIsPersistedWithTheCreatorAsAdmin()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);

        Assert.That(org.Name, Is.EqualTo("My Org"));
        Assert.That(_members.Single().UserId, Is.EqualTo(1));
        Assert.That(_members.Single().Role, Is.EqualTo(OrganizationRole.Admin));
    }

    [Test]
    public async Task WhenCreatingAUsersFirstOrganization_ThenItIsMarkedAsPrimary()
    {
        var org = await _service.CreateAsync("First Org", creatorUserId: 1);

        Assert.That(_members.Single(m => m.OrganizationId == org.Id).IsPrimary, Is.True);
    }

    [Test]
    public async Task WhenCreatingASecondOrganizationForAUser_ThenItIsNotMarkedAsPrimary()
    {
        await _service.CreateAsync("First Org", creatorUserId: 1);

        var second = await _service.CreateAsync("Second Org", creatorUserId: 1);

        Assert.That(_members.Single(m => m.OrganizationId == second.Id).IsPrimary, Is.False);
    }

    [Test]
    public async Task WhenListingOrganizationsForAUser_ThenOnlyThatUsersOrganizationsAreReturnedWithPrimaryFlag()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        await _service.CreateAsync("Other User Org", creatorUserId: 2);

        var results = await _service.ListForUserAsync(1);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Org.Id, Is.EqualTo(org.Id));
        Assert.That(results[0].IsPrimary, Is.True);
    }

    [Test]
    public async Task WhenFindingAnOrganizationByIdThatExists_ThenItIsReturned()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);

        var found = await _service.FindByIdAsync(org.Id);

        Assert.That(found, Is.Not.Null);
    }

    [Test]
    public async Task WhenFindingAnOrganizationByIdThatDoesNotExist_ThenNullIsReturned()
    {
        var found = await _service.FindByIdAsync(999);

        Assert.That(found, Is.Null);
    }

    [Test]
    public void WhenSettingPrimaryForAUserThatIsNotAMember_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.SetPrimaryAsync(999, 1));
    }

    [Test]
    public void WhenUpdatingAnOrganizationThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(999, "renamed"));
    }

    [Test]
    public async Task WhenUpdatingAnOrganization_ThenTheNameIsChanged()
    {
        var org = await _service.CreateAsync("Old Name", creatorUserId: 1);

        var updated = await _service.UpdateAsync(org.Id, "New Name");

        Assert.That(updated.Name, Is.EqualTo("New Name"));
    }

    [Test]
    public void WhenDeletingAnOrganizationThatDoesNotExist_ThenNoExceptionIsThrown()
    {
        Assert.DoesNotThrowAsync(() => _service.DeleteAsync(999));
    }

    [Test]
    public async Task WhenDeletingAnOrganization_ThenItIsRemoved()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);

        await _service.DeleteAsync(org.Id);

        Assert.That(_organizations.Any(o => o.Id == org.Id), Is.False);
    }

    [Test]
    public async Task WhenListingMembers_ThenOnlyThatOrganizationsMembersAreReturned()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        await _service.CreateAsync("Other Org", creatorUserId: 2);

        var members = await _service.ListMembersAsync(org.Id);

        Assert.That(members, Has.Count.EqualTo(1));
        Assert.That(members[0].UserId, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenInvitingAUserWhoIsNotYetAMember_ThenTheyAreAddedWithTheGivenRole()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);

        await _service.InviteUserAsync(org.Id, userId: 2, OrganizationRole.User);

        Assert.That(_members.Single(m => m.UserId == 2).Role, Is.EqualTo(OrganizationRole.User));
    }

    [Test]
    public async Task WhenInvitingAUserWhoIsAlreadyAMember_ThenTheirRoleIsUpdatedWithoutDuplication()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        await _service.InviteUserAsync(org.Id, userId: 2, OrganizationRole.User);

        await _service.InviteUserAsync(org.Id, userId: 2, OrganizationRole.Admin);

        Assert.That(_members.Count(m => m.UserId == 2), Is.EqualTo(1));
        Assert.That(_members.Single(m => m.UserId == 2).Role, Is.EqualTo(OrganizationRole.Admin));
    }

    [Test]
    public void WhenRemovingAMemberThatDoesNotExist_ThenNoExceptionIsThrown()
    {
        Assert.DoesNotThrowAsync(() => _service.RemoveMemberAsync(1, 999));
    }

    [Test]
    public async Task WhenRemovingAMember_ThenTheyAreNoLongerListed()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        await _service.InviteUserAsync(org.Id, userId: 2, OrganizationRole.User);

        await _service.RemoveMemberAsync(org.Id, 2);

        Assert.That(_members.Any(m => m.UserId == 2), Is.False);
    }

    [Test]
    public void WhenGettingAnInviteTokenForAnOrganizationThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetOrCreateInviteTokenAsync(999));
    }

    [Test]
    public async Task WhenGettingAnInviteTokenForTheFirstTime_ThenANewTokenIsGeneratedAndPersisted()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);

        var token = await _service.GetOrCreateInviteTokenAsync(org.Id);

        Assert.That(token, Is.Not.Null.And.Not.Empty);
        Assert.That(_organizations.Single(o => o.Id == org.Id).InviteToken, Is.EqualTo(token));
    }

    [Test]
    public async Task WhenGettingAnInviteTokenASecondTime_ThenTheSameTokenIsReturned()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        var first = await _service.GetOrCreateInviteTokenAsync(org.Id);

        var second = await _service.GetOrCreateInviteTokenAsync(org.Id);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void WhenRegeneratingAnInviteTokenForAnOrganizationThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.RegenerateInviteTokenAsync(999));
    }

    [Test]
    public async Task WhenRegeneratingAnInviteToken_ThenANewTokenReplacesTheOld()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        var first = await _service.GetOrCreateInviteTokenAsync(org.Id);

        var regenerated = await _service.RegenerateInviteTokenAsync(org.Id);

        Assert.That(regenerated, Is.Not.EqualTo(first));
    }

    [Test]
    public async Task WhenFindingAnOrganizationByAValidInviteToken_ThenItIsReturned()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        var token = await _service.GetOrCreateInviteTokenAsync(org.Id);

        var found = await _service.FindByInviteTokenAsync(token);

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Id, Is.EqualTo(org.Id));
    }

    [Test]
    public async Task WhenFindingAnOrganizationByAnInvalidInviteToken_ThenNullIsReturned()
    {
        var found = await _service.FindByInviteTokenAsync("not-a-real-token");

        Assert.That(found, Is.Null);
    }

    private User AddUser(int id, string email, bool isActive = true) => new()
    {
        Id = id,
        Email = email,
        PasswordHash = "hashed",
        FirstName = "Test",
        LastName = "User",
        IsActive = isActive,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Test]
    public async Task WhenInvitingByEmailForAUserThatDoesNotExist_ThenTheResultIndicatesFailure()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);

        var results = await _service.InviteUsersByEmailAsync(org.Id, [new InviteByEmailEntry("nobody@example.com", "User")]);

        Assert.That(results[0].Success, Is.False);
        Assert.That(results[0].Error, Is.Not.Null);
    }

    [Test]
    public async Task WhenInvitingByEmailWithAnInvalidRole_ThenTheResultIndicatesFailure()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        _users.Add(AddUser(2, "bob@example.com"));

        var results = await _service.InviteUsersByEmailAsync(org.Id, [new InviteByEmailEntry("bob@example.com", "NotARole")]);

        Assert.That(results[0].Success, Is.False);
    }

    [Test]
    public async Task WhenInvitingByEmailForAnInactiveUser_ThenTheResultIndicatesFailure()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        _users.Add(AddUser(2, "bob@example.com", isActive: false));

        var results = await _service.InviteUsersByEmailAsync(org.Id, [new InviteByEmailEntry("bob@example.com", "User")]);

        Assert.That(results[0].Success, Is.False);
    }

    [Test]
    public async Task WhenInvitingByEmailForAValidActiveUser_ThenTheyAreAddedAsAMemberAndSuccessIsReturned()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        _users.Add(AddUser(2, "bob@example.com"));

        var results = await _service.InviteUsersByEmailAsync(org.Id, [new InviteByEmailEntry(" BOB@Example.com ", "Admin")]);

        Assert.That(results[0].Success, Is.True);
        Assert.That(_members.Single(m => m.UserId == 2).Role, Is.EqualTo(OrganizationRole.Admin));
    }

    [Test]
    public void WhenAcceptingAnInviteWithAnInvalidToken_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AcceptInviteAsync("not-a-real-token", 2));
    }

    [Test]
    public async Task WhenAcceptingAValidInvite_ThenTheUserIsAddedAsAMemberWithUserRole()
    {
        var org = await _service.CreateAsync("My Org", creatorUserId: 1);
        var token = await _service.GetOrCreateInviteTokenAsync(org.Id);

        await _service.AcceptInviteAsync(token, userId: 2);

        Assert.That(_members.Single(m => m.UserId == 2).Role, Is.EqualTo(OrganizationRole.User));
    }
}
