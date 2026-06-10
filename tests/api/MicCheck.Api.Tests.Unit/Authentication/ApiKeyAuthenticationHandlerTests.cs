using System.Net;
using System.Net.Http.Headers;
using MicCheck.Api.Common.Security.ApiKeys;
using MicCheck.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Authentication;

[TestFixture]
public class ApiKeyAuthenticationHandlerTests
{
    private MicCheckWebApplicationFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new MicCheckWebApplicationFactory();
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    private async Task<string> SeedActiveApiKeyAsync(int organizationId = 1)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();

        var rawKey = ApiKeyHasher.GenerateKey();
        db.ApiKeys.Add(new ApiKey
        {
            Key = ApiKeyHasher.Hash(rawKey),
            Prefix = rawKey[..8],
            Name = "Test Key",
            OrganizationId = organizationId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        return rawKey;
    }

    [Test]
    public async Task WhenAuthorizationHeaderIsAbsent_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/organisations/1/api-keys/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenAuthorizationHeaderIsNotApiKeyScheme_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var response = await client.GetAsync("/api/v1/organisations/1/api-keys/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenApiKeyIsInvalid_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Api-Key not-a-real-key");

        var response = await client.GetAsync("/api/v1/organisations/1/api-keys/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenApiKeyIsInactive_ThenUnauthorizedIsReturned()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();

        var rawKey = ApiKeyHasher.GenerateKey();
        db.ApiKeys.Add(new ApiKey
        {
            Key = ApiKeyHasher.Hash(rawKey),
            Prefix = rawKey[..8],
            Name = "Inactive Key",
            OrganizationId = 1,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Api-Key {rawKey}");

        var response = await client.GetAsync("/api/v1/organisations/1/api-keys/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenApiKeyIsExpired_ThenUnauthorizedIsReturned()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();

        var rawKey = ApiKeyHasher.GenerateKey();
        db.ApiKeys.Add(new ApiKey
        {
            Key = ApiKeyHasher.Hash(rawKey),
            Prefix = rawKey[..8],
            Name = "Expired Key",
            OrganizationId = 1,
            IsActive = true,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
        });
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Api-Key {rawKey}");

        var response = await client.GetAsync("/api/v1/organisations/1/api-keys/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenApiKeyIsValid_ThenOkIsReturned()
    {
        var rawKey = await SeedActiveApiKeyAsync(organizationId: 1);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Api-Key {rawKey}");

        var response = await client.GetAsync("/api/v1/organisations/1/api-keys/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
