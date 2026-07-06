using System.Security.Claims;
using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using MicCheck.Api.Organizations;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Users;
using MicCheck.Api.Webhooks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Organizations;

[TestFixture]
public class OrganizationsControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Organization> _organizations = null!;
    private List<OrganizationUser> _members = null!;
    private List<User> _users = null!;
    private List<Webhook> _webhooks = null!;
    private OrganizationsController _controller = null!;
    private const int UserId = 1;

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
        _webhooks = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Webhooks, _webhooks);

        var webhookQueue = new WebhookQueue();
        var auditService = new Mock<IAuditService>();
        auditService.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _controller = new OrganizationsController(new OrganizationService(_db.Object, auditService.Object), new WebhookService(_db.Object));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId.ToString())], "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private void Unauthenticate() =>
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

    [Test]
    public async Task WhenListingWithoutAuthentication_ThenUnauthorizedIsReturned()
    {
        Unauthenticate();

        var result = await _controller.List();

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task WhenListingOrganizationsForTheCurrentUser_ThenTheyAreReturnedInAPage()
    {
        await _controller.Create(new CreateOrganizationRequest("Org A"), CancellationToken.None);
        await _controller.Create(new CreateOrganizationRequest("Org B"), CancellationToken.None);

        var result = await _controller.List();

        var ok = result.Result as OkObjectResult;
        var page = (PaginatedResponse<OrganizationResponse>)ok!.Value!;
        Assert.That(page.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task WhenCreatingWithoutAuthentication_ThenUnauthorizedIsReturned()
    {
        Unauthenticate();

        var result = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task WhenCreatingAnOrganization_ThenACreatedResultWithTheOrganizationIsReturned()
    {
        var result = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(((OrganizationResponse)created!.Value!).Name, Is.EqualTo("My Org"));
    }

    [Test]
    public async Task WhenGettingAnOrganizationByIdThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetById(999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingAnOrganizationByIdThatExists_ThenItIsReturned()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.GetById(orgId, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task WhenUpdatingAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Update(999, new UpdateOrganizationRequest("renamed"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAnOrganization_ThenTheUpdatedOrganizationIsReturned()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("Old Name"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Update(orgId, new UpdateOrganizationRequest("New Name"), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(((OrganizationResponse)ok!.Value!).Name, Is.EqualTo("New Name"));
    }

    [Test]
    public async Task WhenSettingPrimaryWithoutAuthentication_ThenUnauthorizedIsReturned()
    {
        Unauthenticate();

        var result = await _controller.SetPrimary(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task WhenSettingPrimaryForAUserThatIsNotAMember_ThenNotFoundIsReturned()
    {
        var result = await _controller.SetPrimary(999, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Delete(999, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAnOrganization_ThenNoContentIsReturnedAndItIsRemoved()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Delete(orgId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_organizations.Any(o => o.Id == orgId), Is.False);
    }

    [Test]
    public async Task WhenListingUsersForAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListUsers(999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingUsersForAnOrganization_ThenMembersAreReturned()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        var user = new User { Id = UserId, Email = "alice@example.com", PasswordHash = "hashed", FirstName = "Alice", LastName = "Smith", CreatedAt = DateTimeOffset.UtcNow };
        _users.Add(user);
        var member = _members.Single(m => m.OrganizationId == orgId);
        typeof(OrganizationUser).GetProperty(nameof(OrganizationUser.User))!.SetValue(member, user);

        var result = await _controller.ListUsers(orgId, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var members = (IReadOnlyList<OrganizationMemberResponse>)ok!.Value!;
        Assert.That(members, Has.Count.EqualTo(1));
        Assert.That(members[0].Email, Is.EqualTo("alice@example.com"));
    }

    [Test]
    public async Task WhenInvitingAUserToAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.InviteUser(999, new InviteUserRequest(2, "User"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenInvitingAUser_ThenOkIsReturnedAndTheyBecomeAMember()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.InviteUser(orgId, new InviteUserRequest(2, "Admin"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(_members.Single(m => m.UserId == 2).Role, Is.EqualTo(OrganizationRole.Admin));
    }

    [Test]
    public async Task WhenInvitingUsersByEmailForAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.InviteUsersByEmail(999, new InviteUsersByEmailRequest([]), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenInvitingUsersByEmail_ThenResultsAreReturnedForEachEntry()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.InviteUsersByEmail(
            orgId, new InviteUsersByEmailRequest([new InviteByEmailEntry("nobody@example.com", "User")]), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var results = (IReadOnlyList<InviteByEmailResult>)ok!.Value!;
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Success, Is.False);
    }

    [Test]
    public async Task WhenGettingInviteLinkForAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetInviteLink(999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingInviteLink_ThenATokenIsReturned()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.GetInviteLink(orgId, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(((InviteTokenResponse)ok!.Value!).Token, Is.Not.Empty);
    }

    [Test]
    public async Task WhenRegeneratingInviteLinkForAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.RegenerateInviteLink(999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenRegeneratingInviteLink_ThenANewTokenIsReturned()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        var first = await _controller.GetInviteLink(orgId, CancellationToken.None);
        var firstToken = ((InviteTokenResponse)((OkObjectResult)first.Result!).Value!).Token;

        var result = await _controller.RegenerateInviteLink(orgId, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(((InviteTokenResponse)ok!.Value!).Token, Is.Not.EqualTo(firstToken));
    }

    [Test]
    public async Task WhenAcceptingInviteWithoutAuthentication_ThenUnauthorizedIsReturned()
    {
        Unauthenticate();

        var result = await _controller.AcceptInvite("some-token", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task WhenAcceptingInviteWithAnInvalidToken_ThenNotFoundIsReturned()
    {
        var result = await _controller.AcceptInvite("not-a-real-token", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task WhenAcceptingAValidInvite_ThenOkIsReturnedAndTheUserBecomesAMember()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        var link = await _controller.GetInviteLink(orgId, CancellationToken.None);
        var token = ((InviteTokenResponse)((OkObjectResult)link.Result!).Value!).Token;

        Unauthenticate();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "2")], "TestAuth");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        var result = await _controller.AcceptInvite(token, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(_members.Any(m => m.UserId == 2), Is.True);
    }

    [Test]
    public async Task WhenRemovingUserFromAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.RemoveUser(999, 2, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenRemovingAUser_ThenNoContentIsReturnedAndTheyAreNoLongerAMember()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        await _controller.InviteUser(orgId, new InviteUserRequest(2, "User"), CancellationToken.None);

        var result = await _controller.RemoveUser(orgId, 2, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_members.Any(m => m.UserId == 2), Is.False);
    }

    [Test]
    public async Task WhenListingWebhooksForAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListWebhooks(999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenCreatingAWebhookForAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.CreateWebhook(999, new CreateWebhookRequest("https://example.com", null, true), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenCreatingAWebhook_ThenACreatedResultWithTheWebhookIsReturned()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.CreateWebhook(orgId, new CreateWebhookRequest("https://example.com", "secret", true), CancellationToken.None);

        var webhookCreated = result.Result as CreatedAtActionResult;
        Assert.That(webhookCreated, Is.Not.Null);
        Assert.That(((WebhookResponse)webhookCreated!.Value!).Url, Is.EqualTo("https://example.com"));
    }

    [Test]
    public async Task WhenListingWebhooksForAnOrganization_ThenTheyAreReturned()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        await _controller.CreateWebhook(orgId, new CreateWebhookRequest("https://example.com", null, true), CancellationToken.None);

        var result = await _controller.ListWebhooks(orgId, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var webhooks = (IReadOnlyList<WebhookResponse>)ok!.Value!;
        Assert.That(webhooks, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenUpdatingAWebhookForAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.UpdateWebhook(999, 1, new CreateWebhookRequest("https://example.com", null, true), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAWebhookThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.UpdateWebhook(orgId, 999, new CreateWebhookRequest("https://example.com", null, true), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAWebhookThatBelongsToADifferentOrganization_ThenNotFoundIsReturned()
    {
        var org1 = await _controller.Create(new CreateOrganizationRequest("Org 1"), CancellationToken.None);
        var org1Id = ((OrganizationResponse)((CreatedAtActionResult)org1.Result!).Value!).Id;
        var org2 = await _controller.Create(new CreateOrganizationRequest("Org 2"), CancellationToken.None);
        var org2Id = ((OrganizationResponse)((CreatedAtActionResult)org2.Result!).Value!).Id;
        var webhook = await _controller.CreateWebhook(org1Id, new CreateWebhookRequest("https://example.com", null, true), CancellationToken.None);
        var webhookId = ((WebhookResponse)((CreatedAtActionResult)webhook.Result!).Value!).Id;

        var result = await _controller.UpdateWebhook(org2Id, webhookId, new CreateWebhookRequest("https://other.com", null, false), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAWebhook_ThenItIsChanged()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        var webhook = await _controller.CreateWebhook(orgId, new CreateWebhookRequest("https://example.com", null, true), CancellationToken.None);
        var webhookId = ((WebhookResponse)((CreatedAtActionResult)webhook.Result!).Value!).Id;

        var result = await _controller.UpdateWebhook(orgId, webhookId, new CreateWebhookRequest("https://updated.com", null, false), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(((WebhookResponse)ok!.Value!).Url, Is.EqualTo("https://updated.com"));
    }

    [Test]
    public async Task WhenDeletingAWebhookForAnOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.DeleteWebhook(999, 1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAWebhookThatBelongsToADifferentOrganization_ThenNotFoundIsReturned()
    {
        var org1 = await _controller.Create(new CreateOrganizationRequest("Org 1"), CancellationToken.None);
        var org1Id = ((OrganizationResponse)((CreatedAtActionResult)org1.Result!).Value!).Id;
        var org2 = await _controller.Create(new CreateOrganizationRequest("Org 2"), CancellationToken.None);
        var org2Id = ((OrganizationResponse)((CreatedAtActionResult)org2.Result!).Value!).Id;
        var webhook = await _controller.CreateWebhook(org1Id, new CreateWebhookRequest("https://example.com", null, true), CancellationToken.None);
        var webhookId = ((WebhookResponse)((CreatedAtActionResult)webhook.Result!).Value!).Id;

        var result = await _controller.DeleteWebhook(org2Id, webhookId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAWebhook_ThenItIsRemoved()
    {
        var created = await _controller.Create(new CreateOrganizationRequest("My Org"), CancellationToken.None);
        var orgId = ((OrganizationResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        var webhook = await _controller.CreateWebhook(orgId, new CreateWebhookRequest("https://example.com", null, true), CancellationToken.None);
        var webhookId = ((WebhookResponse)((CreatedAtActionResult)webhook.Result!).Value!).Id;

        var result = await _controller.DeleteWebhook(orgId, webhookId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_webhooks.Any(w => w.Id == webhookId), Is.False);
    }
}
