using System.Net;
using System.Net.Http.Json;
using MicCheck.Api.Common.Security.Authorization;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Common.Security.Authorization;

[TestFixture]
public class AuthEndpointsTests
{
    private MicCheckWebApplicationFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new MicCheckWebApplicationFactory();
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task WhenRegisteringWithValidDetails_ThenOkIsReturnedWithTokens()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "alice@example.com", "SecurePass1!", "Alice", "Smith", "Acme Corp"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.That(body?.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(body?.RefreshToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task WhenRegisteringWithDuplicateEmail_ThenConflictIsReturned()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "alice@example.com", "SecurePass1!", "Alice", "Smith", "Acme Corp"));

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "alice@example.com", "AnotherPass1!", "Alice", "Jones", "Other Corp"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task WhenLoginWithValidCredentials_ThenOkIsReturnedWithTokens()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "bob@example.com", "SecurePass1!", "Bob", "Jones", "BobCo"));

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("bob@example.com", "SecurePass1!"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.That(body?.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(body?.RefreshToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task WhenLoginWithWrongPassword_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "carol@example.com", "CorrectPass1!", "Carol", "White", "CarolCo"));

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("carol@example.com", "WrongPassword"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenLoginWithUnknownEmail_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("nobody@example.com", "SomePass1!"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenLoginWithEmptyEmail_ThenBadRequestIsReturned()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("", "SomePass1!"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task WhenRefreshingWithValidToken_ThenOkIsReturnedWithNewTokens()
    {
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "dave@example.com", "SecurePass1!", "Dave", "Brown", "DaveCo"));
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest(registerBody!.RefreshToken));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.That(body?.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(body?.RefreshToken, Is.Not.EqualTo(registerBody.RefreshToken));
    }

    [Test]
    public async Task WhenRefreshingWithInvalidToken_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest("not-a-valid-refresh-token"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenRefreshingWithRevokedToken_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "eve@example.com", "SecurePass1!", "Eve", "Davis", "EveCo"));
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();

        await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest(registerBody!.RefreshToken));

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest(registerBody.RefreshToken));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenLoggingOut_ThenNoContentIsReturned()
    {
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "frank@example.com", "SecurePass1!", "Frank", "Green", "FrankCo"));
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout",
            new LogoutRequest(registerBody!.RefreshToken));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task WhenLoggingOutAndRefreshing_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "grace@example.com", "SecurePass1!", "Grace", "Black", "GraceCo"));
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();

        await client.PostAsJsonAsync("/api/v1/auth/logout",
            new LogoutRequest(registerBody!.RefreshToken));

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest(registerBody.RefreshToken));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
