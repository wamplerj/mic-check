using System.Security.Claims;
using MicCheck.Api.Data;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Users;

[TestFixture]
public class UsersControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<User> _users = null!;
    private UsersController _controller = null!;
    private const int UserId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _users = [];
        _db.SetupDbSet(c => c.Users, _users);

        var passwordHasher = new Mock<IPasswordHasher<User>>();
        passwordHasher
            .Setup(h => h.VerifyHashedPassword(It.IsAny<User>(), "hashed", "correct-password"))
            .Returns(PasswordVerificationResult.Success);
        passwordHasher
            .Setup(h => h.VerifyHashedPassword(It.IsAny<User>(), "hashed", It.Is<string>(p => p != "correct-password")))
            .Returns(PasswordVerificationResult.Failed);
        passwordHasher
            .Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("new-hashed");

        var service = new UserService(_db.Object, passwordHasher.Object);
        _controller = new UsersController(service);
    }

    private User AddUser(int id = UserId, string email = "alice@example.com") => new()
    {
        Id = id,
        Email = email,
        PasswordHash = "hashed",
        FirstName = "Alice",
        LastName = "Smith",
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private void AuthenticateAs(int? userId)
    {
        var claims = userId is null ? [] : new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
        var identity = new ClaimsIdentity(claims, userId is null ? null : "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Test]
    public async Task WhenGettingAUserById_ThenTheMatchingUserIsReturned()
    {
        var user = AddUser();
        _users.Add(user);

        var result = await _controller.GetById(user.Id, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(((UserResponse)ok!.Value!).Id, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task WhenGettingAUserByIdThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetById(999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingTheCurrentUserWithoutAuthentication_ThenUnauthorizedIsReturned()
    {
        AuthenticateAs(null);

        var result = await _controller.GetMe(CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task WhenGettingTheCurrentUser_ThenTheAuthenticatedUsersProfileIsReturned()
    {
        var user = AddUser();
        _users.Add(user);
        AuthenticateAs(user.Id);

        var result = await _controller.GetMe(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(((UserResponse)ok!.Value!).Email, Is.EqualTo(user.Email));
    }

    [Test]
    public async Task WhenUpdatingTheProfileWithoutAuthentication_ThenUnauthorizedIsReturned()
    {
        AuthenticateAs(null);

        var result = await _controller.UpdateMe(new UpdateProfileRequest("Bob", "Jones", "bob@example.com"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task WhenUpdatingTheProfileWithAMissingField_ThenBadRequestIsReturned()
    {
        var user = AddUser();
        _users.Add(user);
        AuthenticateAs(user.Id);

        var result = await _controller.UpdateMe(new UpdateProfileRequest("", "Jones", "bob@example.com"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task WhenUpdatingTheProfileWithAnEmailTakenByAnotherUser_ThenConflictIsReturned()
    {
        var user = AddUser(id: 1, email: "alice@example.com");
        var otherUser = AddUser(id: 2, email: "taken@example.com");
        _users.Add(user);
        _users.Add(otherUser);
        AuthenticateAs(user.Id);

        var result = await _controller.UpdateMe(new UpdateProfileRequest("Alice", "Smith", "taken@example.com"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task WhenUpdatingTheProfileForAUserThatNoLongerExists_ThenNotFoundIsReturned()
    {
        AuthenticateAs(999);

        var result = await _controller.UpdateMe(new UpdateProfileRequest("Bob", "Jones", "bob@example.com"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingTheProfileWithValidData_ThenTheUpdatedProfileIsReturned()
    {
        var user = AddUser();
        _users.Add(user);
        AuthenticateAs(user.Id);

        var result = await _controller.UpdateMe(new UpdateProfileRequest(" Bob ", " Jones ", " bob@example.com "), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var response = (UserResponse)ok!.Value!;
        Assert.That(response.FirstName, Is.EqualTo("Bob"));
        Assert.That(response.LastName, Is.EqualTo("Jones"));
        Assert.That(response.Email, Is.EqualTo("bob@example.com"));
    }

    [Test]
    public async Task WhenChangingPasswordWithoutAuthentication_ThenUnauthorizedIsReturned()
    {
        AuthenticateAs(null);

        var result = await _controller.ChangePassword(new ChangePasswordRequest("current", "new"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task WhenChangingPasswordWithAMissingField_ThenBadRequestIsReturned()
    {
        var user = AddUser();
        _users.Add(user);
        AuthenticateAs(user.Id);

        var result = await _controller.ChangePassword(new ChangePasswordRequest("", "new"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task WhenChangingPasswordWithAnIncorrectCurrentPassword_ThenBadRequestIsReturned()
    {
        var user = AddUser();
        _users.Add(user);
        AuthenticateAs(user.Id);

        var result = await _controller.ChangePassword(new ChangePasswordRequest("wrong-password", "new-password"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task WhenChangingPasswordWithACorrectCurrentPassword_ThenNoContentIsReturned()
    {
        var user = AddUser();
        _users.Add(user);
        AuthenticateAs(user.Id);

        var result = await _controller.ChangePassword(new ChangePasswordRequest("correct-password", "new-password"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }
}
