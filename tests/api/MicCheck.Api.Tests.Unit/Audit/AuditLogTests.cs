using NUnit.Framework;
using MicCheck.Api.Audit;

namespace MicCheck.Api.Tests.Unit.Audit;

[TestFixture]
public class AuditLogTests
{
    [Test]
    public void WhenAnAuditLogIsCreated_ThenRequiredFieldsAreSet()
    {
        var log = new AuditLog
        {
            ResourceType = "Feature",
            ResourceId = "42",
            Action = "Created",
            OrganizationId = 1
        };

        Assert.That(log.ResourceType, Is.EqualTo("Feature"));
        Assert.That(log.ResourceId, Is.EqualTo("42"));
        Assert.That(log.Action, Is.EqualTo("Created"));
        Assert.That(log.OrganizationId, Is.EqualTo(1));
    }

    [Test]
    public void WhenAnAuditLogIsCreated_ThenOptionalFieldsDefaultToNull()
    {
        var log = new AuditLog
        {
            ResourceType = "Feature",
            ResourceId = "1",
            Action = "Deleted",
            OrganizationId = 1
        };

        Assert.That(log.Changes, Is.Null);
        Assert.That(log.ProjectId, Is.Null);
        Assert.That(log.EnvironmentId, Is.Null);
        Assert.That(log.ActorUserId, Is.Null);
    }
}
