using MicCheck.Api.Common.Security.ApiKeys;
using MicCheck.Api.Data;
using MicCheck.Api.Tests.Unit.TestSupport;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Common.Security.ApiKeys;

[TestFixture]
public class ApiKeyServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<ApiKey> _apiKeys = null!;
    private ApiKeyService _service = null!;
    private const int OrganizationId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _apiKeys = [];
        _db.SetupDbSetWithGeneratedIds(c => c.ApiKeys, _apiKeys);

        _service = new ApiKeyService(_db.Object);
    }

    [Test]
    public async Task WhenCreatingAnApiKey_ThenTheStoredKeyIsHashedAndTheRawKeyIsReturnedOnce()
    {
        var result = await _service.CreateAsync(OrganizationId, "CI Key", null);

        Assert.That(result.RawKey, Is.Not.Empty);
        Assert.That(result.Key.Key, Is.EqualTo(ApiKeyHasher.Hash(result.RawKey)));
        Assert.That(result.Key.Key, Is.Not.EqualTo(result.RawKey));
    }

    [Test]
    public async Task WhenCreatingAnApiKey_ThenThePrefixIsTheFirstEightCharactersOfTheRawKey()
    {
        var result = await _service.CreateAsync(OrganizationId, "CI Key", null);

        Assert.That(result.Key.Prefix, Is.EqualTo(result.RawKey[..8]));
    }

    [Test]
    public async Task WhenCreatingAnApiKey_ThenItIsPersistedAsActiveForTheGivenOrganization()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        var result = await _service.CreateAsync(OrganizationId, "CI Key", expiresAt);

        var persisted = _apiKeys.Single();
        Assert.That(persisted.Id, Is.EqualTo(result.Key.Id));
        Assert.That(persisted.Name, Is.EqualTo("CI Key"));
        Assert.That(persisted.OrganizationId, Is.EqualTo(OrganizationId));
        Assert.That(persisted.IsActive, Is.True);
        Assert.That(persisted.ExpiresAt, Is.EqualTo(expiresAt));
    }

    [Test]
    public async Task WhenListingApiKeysForAnOrganization_ThenOnlyThatOrganizationsKeysAreReturned()
    {
        await _service.CreateAsync(OrganizationId, "Org 1 Key", null);
        await _service.CreateAsync(999, "Org 2 Key", null);

        var keys = await _service.ListAsync(OrganizationId);

        Assert.That(keys, Has.Count.EqualTo(1));
        Assert.That(keys[0].Name, Is.EqualTo("Org 1 Key"));
    }

    [Test]
    public async Task WhenRevokingAnExistingApiKey_ThenItIsMarkedInactive()
    {
        var created = await _service.CreateAsync(OrganizationId, "CI Key", null);

        await _service.RevokeAsync(OrganizationId, created.Key.Id);

        Assert.That(_apiKeys.Single().IsActive, Is.False);
    }

    [Test]
    public async Task WhenRevokingAnApiKeyThatBelongsToADifferentOrganization_ThenItIsNotRevoked()
    {
        var created = await _service.CreateAsync(OrganizationId, "CI Key", null);

        await _service.RevokeAsync(999, created.Key.Id);

        Assert.That(_apiKeys.Single().IsActive, Is.True);
    }

    [Test]
    public void WhenRevokingAnApiKeyThatDoesNotExist_ThenNoExceptionIsThrown()
    {
        Assert.DoesNotThrowAsync(() => _service.RevokeAsync(OrganizationId, 999));
    }
}
