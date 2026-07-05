using MicCheck.Api.Identities;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Identities;

[TestFixture]
public class AdminIdentityResponseTests
{
    [Test]
    public void WhenMappingAnIdentityWithNoTraits_ThenTheResponseHasAnEmptyTraitsList()
    {
        var identity = new Identity { Id = 1, Identifier = "user-1", EnvironmentId = 5, CreatedAt = DateTimeOffset.UtcNow };

        var response = AdminIdentityResponse.From(identity);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.Identifier, Is.EqualTo("user-1"));
        Assert.That(response.EnvironmentId, Is.EqualTo(5));
        Assert.That(response.Traits, Is.Empty);
    }

    [Test]
    public void WhenMappingAnIdentityWithTraits_ThenEachTraitIsMapped()
    {
        var identity = new Identity { Id = 1, Identifier = "user-1", EnvironmentId = 1, CreatedAt = DateTimeOffset.UtcNow };
        identity.Traits.Add(new IdentityTrait { Key = "plan", Value = "premium" });

        var response = AdminIdentityResponse.From(identity);

        Assert.That(response.Traits, Has.Count.EqualTo(1));
        Assert.That(response.Traits[0].Key, Is.EqualTo("plan"));
        Assert.That(response.Traits[0].Value, Is.EqualTo("premium"));
    }
}
