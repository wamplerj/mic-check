using MicCheck.Api.Audit;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Audit;

[TestFixture]
public class AuditLogResponseTests
{
    [Test]
    public void WhenMappingWithoutAnActorUserName_ThenItDefaultsToNull()
    {
        var log = new AuditLog
        {
            Id = 1,
            ResourceType = "Feature",
            ResourceId = "42",
            Action = "created",
            OrganizationId = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var response = AuditLogResponse.From(log);

        Assert.That(response.ActorUserName, Is.Null);
    }

    [Test]
    public void WhenMappingWithAnActorUserName_ThenItIsIncluded()
    {
        var log = new AuditLog
        {
            Id = 1,
            ResourceType = "Feature",
            ResourceId = "42",
            Action = "created",
            OrganizationId = 1,
            ActorUserId = 7,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var response = AuditLogResponse.From(log, "Alice Smith");

        Assert.That(response.ActorUserId, Is.EqualTo(7));
        Assert.That(response.ActorUserName, Is.EqualTo("Alice Smith"));
    }

    [Test]
    public void WhenMapping_ThenAllFieldsAreCopiedFromTheLog()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var log = new AuditLog
        {
            Id = 1,
            ResourceType = "FeatureState",
            ResourceId = "5",
            Action = "updated",
            Changes = """{"before":{},"after":{}}""",
            OrganizationId = 10,
            ProjectId = 20,
            EnvironmentId = 30,
            CreatedAt = createdAt
        };

        var response = AuditLogResponse.From(log);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.ResourceType, Is.EqualTo("FeatureState"));
        Assert.That(response.ResourceId, Is.EqualTo("5"));
        Assert.That(response.Action, Is.EqualTo("updated"));
        Assert.That(response.Changes, Is.EqualTo("""{"before":{},"after":{}}"""));
        Assert.That(response.OrganizationId, Is.EqualTo(10));
        Assert.That(response.ProjectId, Is.EqualTo(20));
        Assert.That(response.EnvironmentId, Is.EqualTo(30));
        Assert.That(response.CreatedAt, Is.EqualTo(createdAt));
    }
}
