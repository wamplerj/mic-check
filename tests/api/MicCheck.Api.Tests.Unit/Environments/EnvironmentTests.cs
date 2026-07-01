using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Environments;

[TestFixture]
public class EnvironmentTests
{
    [Test]
    public void WhenAnEnvironmentIsCreated_ThenFeatureStatesCollectionIsInitializedEmpty()
    {
        var environment = new AppEnvironment { Name = "Production", ApiKey = "env-key-abc" };

        Assert.That(environment.FeatureStates, Is.Empty);
    }

    [Test]
    public void WhenAnEnvironmentIsCreated_ThenNameAndApiKeyAreSet()
    {
        var environment = new AppEnvironment { Name = "Staging", ApiKey = "env-key-xyz" };

        Assert.That(environment.Name, Is.EqualTo("Staging"));
        Assert.That(environment.ApiKey, Is.EqualTo("env-key-xyz"));
    }
}
