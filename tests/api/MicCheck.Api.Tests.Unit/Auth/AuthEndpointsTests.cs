using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using MicCheck.Api.Auth;

namespace MicCheck.Api.Tests.Unit.Auth;

[TestFixture]
public class AuthEndpointsTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private Mock<ITokenService> _tokenService = null!;

    [SetUp]
    public void SetUp()
    {
        _tokenService = new Mock<ITokenService>();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.ConfigureServices(services =>
                {
                    services.AddScoped<ITokenService>(_ => _tokenService.Object);
                });
            });
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task WhenUsernameIsEmpty_ThenBadRequestIsReturned()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/token",
            new TokenRequest("", "password123"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task WhenPasswordIsEmpty_ThenBadRequestIsReturned()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/token",
            new TokenRequest("testuser", ""));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task WhenValidCredentialsAreProvided_ThenOkIsReturnedWithToken()
    {
        var expectedResponse = new TokenResponse("signed.jwt.token", DateTime.UtcNow.AddHours(1));
        _tokenService
            .Setup(s => s.GenerateToken("testuser"))
            .Returns(expectedResponse);

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/token",
            new TokenRequest("testuser", "password123"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.That(body?.Token, Is.EqualTo(expectedResponse.Token));
    }
}
