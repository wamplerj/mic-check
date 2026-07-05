using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Data;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Common.Security.Authorization;

[TestFixture]
public class ProjectPermissionRequirementHandlerTests
{
    private List<UserProjectPermission> _userProjectPermissions = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessor = null!;
    private ProjectPermissionRequirementHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var db = new Mock<IMicCheckDbContext>();
        _userProjectPermissions = [];
        db.SetupDbSet(c => c.UserProjectPermissions, _userProjectPermissions);

        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _handler = new ProjectPermissionRequirementHandler(db.Object, _httpContextAccessor.Object);
    }

    private static AuthorizationHandlerContext CreateContext(
        ClaimsPrincipal user,
        ProjectPermission permission = ProjectPermission.ViewProject)
    {
        var requirement = new ProjectPermissionRequirement(permission);
        return new AuthorizationHandlerContext([requirement], user, null);
    }

    private static ClaimsPrincipal CreateUserWithClaims(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private void SetupRouteProjectId(int projectId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["projectId"] = projectId.ToString();
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
    }

    [Test]
    public async Task WhenUserIsOrganizationAdmin_ThenRequirementSucceeds()
    {
        var user = CreateUserWithClaims(
            new Claim(JwtRegisteredClaimNames.Sub, "1"),
            new Claim("OrganizationRole", "Admin"));

        var context = CreateContext(user);

        await _handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task WhenUserHasRequiredPermission_ThenRequirementSucceeds()
    {
        _userProjectPermissions.Add(new UserProjectPermission
        {
            UserId = 1,
            ProjectId = 10,
            Permissions = [ProjectPermission.ViewProject]
        });

        SetupRouteProjectId(10);

        var user = CreateUserWithClaims(
            new Claim(JwtRegisteredClaimNames.Sub, "1"));

        var context = CreateContext(user, ProjectPermission.ViewProject);

        await _handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task WhenUserIsProjectAdmin_ThenAllPermissionsAreGranted()
    {
        _userProjectPermissions.Add(new UserProjectPermission
        {
            UserId = 2,
            ProjectId = 20,
            IsAdmin = true,
            Permissions = []
        });

        SetupRouteProjectId(20);

        var user = CreateUserWithClaims(
            new Claim(JwtRegisteredClaimNames.Sub, "2"));

        var context = CreateContext(user, ProjectPermission.DeleteFeature);

        await _handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task WhenUserDoesNotHaveRequiredPermission_ThenRequirementFails()
    {
        _userProjectPermissions.Add(new UserProjectPermission
        {
            UserId = 3,
            ProjectId = 30,
            Permissions = [ProjectPermission.ViewProject]
        });

        SetupRouteProjectId(30);

        var user = CreateUserWithClaims(
            new Claim(JwtRegisteredClaimNames.Sub, "3"));

        var context = CreateContext(user, ProjectPermission.DeleteFeature);

        await _handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task WhenUserHasNoProjectPermissionRecord_ThenRequirementFails()
    {
        SetupRouteProjectId(99);

        var user = CreateUserWithClaims(
            new Claim(JwtRegisteredClaimNames.Sub, "5"));

        var context = CreateContext(user);

        await _handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task WhenUserIdClaimIsMissing_ThenRequirementFails()
    {
        var user = CreateUserWithClaims();

        var context = CreateContext(user);

        await _handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task WhenProjectIdIsNotInRoute_ThenRequirementFails()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());

        var user = CreateUserWithClaims(
            new Claim(JwtRegisteredClaimNames.Sub, "1"));

        var context = CreateContext(user);

        await _handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }
}
