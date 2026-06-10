using System.IdentityModel.Tokens.Jwt;
using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Organizations;
using MicCheck.Api.Users;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Common.Security.Authorization;

[TestFixture]
public class TokenServiceTests
{
    private IConfiguration _configuration = null!;
    private TokenService _tokenService = null!;

    [SetUp]
    public void SetUp()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-that-is-long-enough-32c",
                ["Jwt:Issuer"] = "MicCheck",
                ["Jwt:Audience"] = "MicCheck",
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    private static User BuildUser(int id = 1, string email = "alice@example.com") =>
        new()
        {
            Email = email,
            FirstName = "Alice",
            LastName = "Smith",
            PasswordHash = "hashed",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

    [Test]
    public void WhenAUserIsProvided_ThenATokenIsReturned()
    {
        var result = _tokenService.GenerateToken(BuildUser());

        Assert.That(result.Token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void WhenATokenIsGenerated_ThenItContainsTheUserEmailClaim()
    {
        var user = BuildUser(email: "bob@example.com");

        var result = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        Assert.That(jwt.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "bob@example.com"),
            Is.True);
    }

    [Test]
    public void WhenATokenIsGenerated_ThenItHasAFutureExpiry()
    {
        var result = _tokenService.GenerateToken(BuildUser());

        Assert.That(result.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
    }

    [Test]
    public void WhenATokenIsGenerated_ThenItHasTheConfiguredIssuer()
    {
        var result = _tokenService.GenerateToken(BuildUser());

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        Assert.That(jwt.Issuer, Is.EqualTo("MicCheck"));
    }

    [Test]
    public void WhenUserBelongsToOrganizations_ThenTokenContainsOrganizationClaims()
    {
        var user = BuildUser();
        user.Organizations.Add(new OrganizationUser
        {
            OrganizationId = 42,
            UserId = user.Id,
            Role = OrganizationRole.Admin
        });

        var result = _tokenService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        Assert.That(jwt.Claims.Any(c => c.Type == "OrganizationId" && c.Value == "42"), Is.True);
        Assert.That(jwt.Claims.Any(c => c.Type == "OrganizationRole" && c.Value == "Admin"), Is.True);
    }
}
