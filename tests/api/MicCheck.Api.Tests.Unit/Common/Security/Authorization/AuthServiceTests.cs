using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Data;
using MicCheck.Api.Organizations;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Users;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Common.Security.Authorization;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<User> _users = null!;
    private List<Organization> _organizations = null!;
    private List<OrganizationUser> _organizationUsers = null!;
    private List<RefreshToken> _refreshTokens = null!;
    private PasswordHasher<User> _passwordHasher = null!;
    private AuthService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _users = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Users, _users);
        _organizations = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Organizations, _organizations);
        _organizationUsers = [];
        _db.SetupDbSet(c => c.OrganizationUsers, _organizationUsers);
        _refreshTokens = [];
        _db.SetupDbSetWithGeneratedIds(c => c.RefreshTokens, _refreshTokens);

        _passwordHasher = new PasswordHasher<User>();

        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Returns(new TokenResponse("access-token", DateTime.UtcNow.AddHours(1)));

        _service = new AuthService(_db.Object, tokenService.Object, _passwordHasher);
    }

    private User AddUser(string email, string password, bool isActive = true)
    {
        var user = new User
        {
            Email = email,
            FirstName = "First",
            LastName = "Last",
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow,
            PasswordHash = string.Empty
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        _db.Object.Users.Add(user);
        return user;
    }

    [Test]
    public async Task WhenRegisteringWithValidDetails_ThenTokensAreReturned()
    {
        var response = await _service.RegisterAsync(
            "alice@example.com", "SecurePass1!", "Alice", "Smith", "Acme Corp");

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(response.RefreshToken, Is.Not.Null.And.Not.Empty);
        Assert.That(_users, Has.Count.EqualTo(1));
        Assert.That(_organizations, Has.Count.EqualTo(1));
        Assert.That(_organizationUsers.Single().Role, Is.EqualTo(OrganizationRole.Admin));
    }

    [Test]
    public async Task WhenRegisteringWithDuplicateEmail_ThenNullIsReturned()
    {
        AddUser("alice@example.com", "SecurePass1!");

        var response = await _service.RegisterAsync(
            "alice@example.com", "AnotherPass1!", "Alice", "Jones", "Other Corp");

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task WhenLoginWithValidCredentials_ThenTokensAreReturned()
    {
        AddUser("bob@example.com", "SecurePass1!");

        var response = await _service.LoginAsync("bob@example.com", "SecurePass1!");

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(response.RefreshToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task WhenLoginWithWrongPassword_ThenNullIsReturned()
    {
        AddUser("carol@example.com", "CorrectPass1!");

        var response = await _service.LoginAsync("carol@example.com", "WrongPassword");

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task WhenLoginWithUnknownEmail_ThenNullIsReturned()
    {
        var response = await _service.LoginAsync("nobody@example.com", "SomePass1!");

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task WhenRefreshingWithValidToken_ThenNewTokensAreReturnedAndOldTokenIsRevoked()
    {
        var user = AddUser("dave@example.com", "SecurePass1!");
        var refreshToken = new RefreshToken
        {
            Token = "valid-refresh-token",
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Object.RefreshTokens.Add(refreshToken);

        var response = await _service.RefreshAsync("valid-refresh-token");

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.RefreshToken, Is.Not.EqualTo("valid-refresh-token"));
        Assert.That(refreshToken.IsRevoked, Is.True);
    }

    [Test]
    public async Task WhenRefreshingWithInvalidToken_ThenNullIsReturned()
    {
        var response = await _service.RefreshAsync("not-a-valid-refresh-token");

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task WhenRefreshingWithRevokedToken_ThenNullIsReturned()
    {
        var user = AddUser("eve@example.com", "SecurePass1!");
        _db.Object.RefreshTokens.Add(new RefreshToken
        {
            Token = "revoked-token",
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            IsRevoked = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var response = await _service.RefreshAsync("revoked-token");

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task WhenRefreshingWithExpiredToken_ThenNullIsReturned()
    {
        var user = AddUser("frank@example.com", "SecurePass1!");
        _db.Object.RefreshTokens.Add(new RefreshToken
        {
            Token = "expired-token",
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-31)
        });

        var response = await _service.RefreshAsync("expired-token");

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task WhenLoggingOut_ThenTokenIsRevoked()
    {
        var user = AddUser("grace@example.com", "SecurePass1!");
        var refreshToken = new RefreshToken
        {
            Token = "logout-token",
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Object.RefreshTokens.Add(refreshToken);

        await _service.LogoutAsync("logout-token");

        Assert.That(refreshToken.IsRevoked, Is.True);
    }

    [Test]
    public async Task WhenLoggingOutAndRefreshing_ThenNullIsReturned()
    {
        var user = AddUser("henry@example.com", "SecurePass1!");
        _db.Object.RefreshTokens.Add(new RefreshToken
        {
            Token = "logout-then-refresh-token",
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _service.LogoutAsync("logout-then-refresh-token");
        var response = await _service.RefreshAsync("logout-then-refresh-token");

        Assert.That(response, Is.Null);
    }
}
