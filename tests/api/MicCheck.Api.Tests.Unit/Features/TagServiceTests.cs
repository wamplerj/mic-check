using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Tests.Unit.TestSupport;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class TagServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Tag> _tags = null!;
    private TagService _service = null!;
    private const int ProjectId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _tags = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Tags, _tags);

        _service = new TagService(_db.Object);
    }

    [Test]
    public async Task WhenCreatingATag_ThenItIsPersistedWithTheGivenProject()
    {
        var tag = await _service.CreateAsync(ProjectId, "Beta", "#FF0000");

        Assert.That(tag.Label, Is.EqualTo("Beta"));
        Assert.That(tag.Color, Is.EqualTo("#FF0000"));
        Assert.That(tag.ProjectId, Is.EqualTo(ProjectId));
        Assert.That(_tags, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenListingTagsForAProject_ThenOnlyThatProjectsTagsAreReturned()
    {
        await _service.CreateAsync(ProjectId, "Beta", "#FF0000");
        await _service.CreateAsync(999, "Other", "#00FF00");

        var tags = await _service.ListByProjectAsync(ProjectId);

        Assert.That(tags, Has.Count.EqualTo(1));
        Assert.That(tags[0].Label, Is.EqualTo("Beta"));
    }

    [Test]
    public async Task WhenFindingATagByIdThatExists_ThenTheTagIsReturned()
    {
        var tag = await _service.CreateAsync(ProjectId, "Beta", "#FF0000");

        var found = await _service.FindByIdAsync(tag.Id);

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Id, Is.EqualTo(tag.Id));
    }

    [Test]
    public async Task WhenFindingATagByIdThatDoesNotExist_ThenNullIsReturned()
    {
        var found = await _service.FindByIdAsync(999);

        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task WhenDeletingATag_ThenItIsRemovedFromTheDatabase()
    {
        var tag = await _service.CreateAsync(ProjectId, "Beta", "#FF0000");

        await _service.DeleteAsync(tag.Id);

        Assert.That(_tags.Any(t => t.Id == tag.Id), Is.False);
    }

    [Test]
    public void WhenDeletingATagThatDoesNotExist_ThenNoExceptionIsThrown()
    {
        Assert.DoesNotThrowAsync(() => _service.DeleteAsync(999));
    }
}
