using NUnit.Framework;
using MicCheck.Api.Projects;

namespace MicCheck.Api.Tests.Unit.Projects;

[TestFixture]
public class ProjectTests
{
    [Test]
    public void WhenAProjectIsCreated_ThenCollectionsAreInitializedEmpty()
    {
        var project = new Project { Name = "My Project", OrganizationId = 1 };

        Assert.That(project.Environments, Is.Empty);
        Assert.That(project.Features, Is.Empty);
        Assert.That(project.Segments, Is.Empty);
    }

    [Test]
    public void WhenAProjectIsCreated_ThenHideDisabledFlagsDefaultsToFalse()
    {
        var project = new Project { Name = "My Project", OrganizationId = 1 };

        Assert.That(project.HideDisabledFlags, Is.False);
    }

    [Test]
    public void WhenAProjectHideDisabledFlagsIsEnabled_ThenThePropertyReflectsTheChange()
    {
        var project = new Project { Name = "My Project", OrganizationId = 1 };

        project.HideDisabledFlags = true;

        Assert.That(project.HideDisabledFlags, Is.True);
    }
}
