using System.Text.Encodings.Web;
using MicCheck.Api.Common.Security.ApiKeys;
using MicCheck.Api.Common.Security.Authentication;
using MicCheck.Api.Data;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Common.Security.Authentication;

[TestFixture]
public class ApiKeyAuthenticationHandlerTests
{
    private List<ApiKey> _apiKeys = null!;
    private Mock<IMicCheckDbContext> _db = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _apiKeys = [];
        _db.SetupDbSet(c => c.ApiKeys, _apiKeys);
    }

    private async Task<AuthenticateResult> AuthenticateAsync(string? authorizationHeader)
    {
        var optionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Setup(o => o.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

        var handler = new ApiKeyAuthenticationHandler(optionsMonitor.Object, NullLoggerFactory.Instance, UrlEncoder.Default, _db.Object);

        var httpContext = new DefaultHttpContext();
        if (authorizationHeader is not null)
            httpContext.Request.Headers.Authorization = authorizationHeader;

        var scheme = new AuthenticationScheme(ApiKeyAuthenticationHandler.SchemeName, null, typeof(ApiKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, httpContext);

        return await handler.AuthenticateAsync();
    }

    private ApiKey AddApiKey(string rawKey, int organizationId = 1, bool isActive = true, DateTimeOffset? expiresAt = null)
    {
        var apiKey = new ApiKey
        {
            Key = ApiKeyHasher.Hash(rawKey),
            Prefix = rawKey[..8],
            Name = "Test Key",
            OrganizationId = organizationId,
            IsActive = isActive,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _apiKeys.Add(apiKey);
        return apiKey;
    }

    [Test]
    public async Task WhenAuthorizationHeaderIsAbsent_ThenNoResultIsReturned()
    {
        var result = await AuthenticateAsync(null);

        Assert.That(result.None, Is.True);
    }

    [Test]
    public async Task WhenAuthorizationHeaderIsNotApiKeyScheme_ThenNoResultIsReturned()
    {
        var result = await AuthenticateAsync("Bearer not-a-jwt");

        Assert.That(result.None, Is.True);
    }

    [Test]
    public async Task WhenApiKeyIsInvalid_ThenAuthenticationFails()
    {
        var result = await AuthenticateAsync("Api-Key not-a-real-key");

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task WhenApiKeyIsInactive_ThenAuthenticationFails()
    {
        var rawKey = ApiKeyHasher.GenerateKey();
        AddApiKey(rawKey, isActive: false);

        var result = await AuthenticateAsync($"Api-Key {rawKey}");

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task WhenApiKeyIsExpired_ThenAuthenticationFails()
    {
        var rawKey = ApiKeyHasher.GenerateKey();
        AddApiKey(rawKey, expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        var result = await AuthenticateAsync($"Api-Key {rawKey}");

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task WhenApiKeyIsValid_ThenAuthenticationSucceedsWithOrganizationClaims()
    {
        var rawKey = ApiKeyHasher.GenerateKey();
        AddApiKey(rawKey, organizationId: 42);

        var result = await AuthenticateAsync($"Api-Key {rawKey}");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Principal!.FindFirst("OrganizationId")!.Value, Is.EqualTo("42"));
        Assert.That(result.Principal!.FindFirst("OrganizationRole")!.Value, Is.EqualTo("Admin"));
    }
}
