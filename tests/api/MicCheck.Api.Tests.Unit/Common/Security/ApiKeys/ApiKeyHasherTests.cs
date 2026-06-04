using MicCheck.Api.Common.Security.ApiKeys;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Common.Security.ApiKeys;

[TestFixture]
public class ApiKeyHasherTests
{
    [Test]
    public void WhenKeyIsHashed_ThenHashIsDeterministic()
    {
        var key = "test-api-key";

        var hash1 = ApiKeyHasher.Hash(key);
        var hash2 = ApiKeyHasher.Hash(key);

        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void WhenKeyIsHashed_ThenHashIsLowercaseHex()
    {
        var hash = ApiKeyHasher.Hash("any-key");

        Assert.That(hash, Does.Match("^[0-9a-f]{64}$"));
    }

    [Test]
    public void WhenDifferentKeysAreHashed_ThenHashesDiffer()
    {
        var hash1 = ApiKeyHasher.Hash("key-one");
        var hash2 = ApiKeyHasher.Hash("key-two");

        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void WhenGenerateKeyIsCalled_ThenKeyIsNotEmpty()
    {
        var key = ApiKeyHasher.GenerateKey();

        Assert.That(key, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void WhenGenerateKeyIsCalled_ThenEachKeyIsUnique()
    {
        var key1 = ApiKeyHasher.GenerateKey();
        var key2 = ApiKeyHasher.GenerateKey();

        Assert.That(key1, Is.Not.EqualTo(key2));
    }

    [Test]
    public void WhenGenerateKeyIsCalled_ThenKeyIsBase64UrlSafe()
    {
        var key = ApiKeyHasher.GenerateKey();

        Assert.That(key, Does.Not.Contain("+"));
        Assert.That(key, Does.Not.Contain("/"));
        Assert.That(key, Does.Not.Contain("="));
    }

    [Test]
    public void WhenGenerateKeyIsHashed_ThenHashCanBeUsedToVerify()
    {
        var rawKey = ApiKeyHasher.GenerateKey();
        var hash = ApiKeyHasher.Hash(rawKey);

        Assert.That(ApiKeyHasher.Hash(rawKey), Is.EqualTo(hash));
    }
}
