using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using MicCheck.Api.Auth;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Auth;

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

    [Test]
    public void WhenAUsernameIsProvided_ThenATokenIsReturned()
    {
        var result = _tokenService.GenerateToken("testuser");

        Assert.That(result.Token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void WhenATokenIsGenerated_ThenItContainsTheUsernameAsSubjectClaim()
    {
        var result = _tokenService.GenerateToken("testuser");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        Assert.That(jwt.Subject, Is.EqualTo("testuser"));
    }

    [Test]
    public void WhenATokenIsGenerated_ThenItHasAFutureExpiry()
    {
        var result = _tokenService.GenerateToken("testuser");

        Assert.That(result.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
    }

    [Test]
    public void WhenATokenIsGenerated_ThenItHasTheConfiguredIssuer()
    {
        var result = _tokenService.GenerateToken("testuser");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        Assert.That(jwt.Issuer, Is.EqualTo("MicCheck"));
    }
}
