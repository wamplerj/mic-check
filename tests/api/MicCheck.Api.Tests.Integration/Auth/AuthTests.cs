using System.Text.Json;
using MicCheck.Api.Tests.Integration.Common;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Integration.Auth;

[TestFixture]
public class AuthTests
{
    private const string Password = "Integration!Test123";

    private IntegrationTestSettings settings = null!;
    private TestDatabase db = null!;
    private FlagApiHttpClient client = null!;
    private AuthSeed? seed;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        settings = IntegrationTestSettings.Load();
        db = new TestDatabase(settings.DbConnectionString);
        client = new FlagApiHttpClient(settings);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => client.Dispose();

    [SetUp]
    public async Task SetUp()
        => seed = await AuthSeed.InsertAsync(db, testRunTag: "Auth", password: Password);

    [TearDown]
    public async Task TearDown()
    {
        if (seed is not null)
        {
            await seed.CleanupAsync(db);
            seed = null;
        }
    }

    [Test]
    public async Task WhenValidCredentialsAreSubmitted_ThenLoginReturnsAnAccessToken()
    {
        var current = seed!;

        using var response = await client.SendAsync(
            httpFile: "Auth.http",
            requestName: "Login",
            variables: new Dictionary<string, string>
            {
                ["baseUrl"] = settings.BaseUrl,
                ["email"] = current.Email,
                ["password"] = current.Password
            });

        var body = await response.Content.ReadAsStringAsync();

        Assert.That(
            response.IsSuccessStatusCode,
            Is.True,
            $"POST /api/v1/auth/login returned {(int)response.StatusCode} {response.ReasonPhrase} for a valid seeded user. Body: {body}");

        var login = JsonSerializer.Deserialize<LoginResponseDto>(body, JsonOptions)
            ?? throw new InvalidOperationException("Response body was not the expected login shape.");

        Assert.That(login.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(login.RefreshToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task WhenAnIncorrectPasswordIsSubmitted_ThenLoginIsUnauthorized()
    {
        var current = seed!;

        using var response = await client.SendAsync(
            httpFile: "Auth.http",
            requestName: "Login",
            variables: new Dictionary<string, string>
            {
                ["baseUrl"] = settings.BaseUrl,
                ["email"] = current.Email,
                ["password"] = "definitely-the-wrong-password"
            });

        Assert.That(
            (int)response.StatusCode,
            Is.EqualTo(401),
            $"Expected 401 for an incorrect password, got {(int)response.StatusCode} {response.ReasonPhrase}.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
