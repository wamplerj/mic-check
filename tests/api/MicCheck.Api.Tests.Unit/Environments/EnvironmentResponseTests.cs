using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Environments;

[TestFixture]
public class EnvironmentResponseTests
{
    [Test]
    public void WhenMappingAnEnvironment_ThenAllFieldsAreCopied()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var environment = new AppEnvironment
        {
            Id = 1,
            Name = "Production",
            ApiKey = "env-key-abc",
            ProjectId = 2,
            CreatedAt = createdAt
        };

        var response = MicCheck.Api.Environments.EnvironmentResponse.From(environment);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.Name, Is.EqualTo("Production"));
        Assert.That(response.ApiKey, Is.EqualTo("env-key-abc"));
        Assert.That(response.ProjectId, Is.EqualTo(2));
        Assert.That(response.CreatedAt, Is.EqualTo(createdAt));
    }
}
