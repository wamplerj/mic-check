using NUnit.Framework;
using MicCheck.Api.Common.Security.ApiKeys;

namespace MicCheck.Api.Tests.Unit.ApiKeys;

[TestFixture]
public class ApiKeyTests
{
    [Test]
    public void WhenAnApiKeyIsCreated_ThenRequiredFieldsAreSet()
    {
        var apiKey = new ApiKey
        {
            Key = "hashed-value",
            Prefix = "abc12345",
            Name = "CI Pipeline Key",
            OrganizationId = 1
        };

        Assert.That(apiKey.Key, Is.EqualTo("hashed-value"));
        Assert.That(apiKey.Prefix, Is.EqualTo("hashed-value".Substring(0, 8)).Or.EqualTo("abc12345"));
        Assert.That(apiKey.Name, Is.EqualTo("CI Pipeline Key"));
        Assert.That(apiKey.OrganizationId, Is.EqualTo(1));
    }

    [Test]
    public void WhenAnApiKeyIsCreated_ThenIsActiveDefaultsToFalse()
    {
        var apiKey = new ApiKey
        {
            Key = "hashed-value",
            Prefix = "abc12345",
            Name = "Test Key",
            OrganizationId = 1
        };

        Assert.That(apiKey.IsActive, Is.False);
    }

    [Test]
    public void WhenAnApiKeyIsCreated_ThenExpiresAtDefaultsToNull()
    {
        var apiKey = new ApiKey
        {
            Key = "hashed-value",
            Prefix = "abc12345",
            Name = "Test Key",
            OrganizationId = 1
        };

        Assert.That(apiKey.ExpiresAt, Is.Null);
    }
}
