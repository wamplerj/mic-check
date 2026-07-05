using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class TagsControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Tag> _tags = null!;
    private TagsController _controller = null!;
    private const int ProjectId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _tags = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Tags, _tags);

        _controller = new TagsController(new TagService(_db.Object));
    }

    [Test]
    public async Task WhenListingTagsForAProject_ThenAllOfThatProjectsTagsAreReturned()
    {
        await _controller.Create(ProjectId, new CreateTagRequest("Beta", "#FF0000"), CancellationToken.None);
        await _controller.Create(999, new CreateTagRequest("Other", "#00FF00"), CancellationToken.None);

        var result = await _controller.List(ProjectId, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var tags = (IReadOnlyList<TagResponse>)ok!.Value!;
        Assert.That(tags, Has.Count.EqualTo(1));
        Assert.That(tags[0].Label, Is.EqualTo("Beta"));
    }

    [Test]
    public async Task WhenCreatingATag_ThenACreatedResultWithTheTagIsReturned()
    {
        var result = await _controller.Create(ProjectId, new CreateTagRequest("Beta", "#FF0000"), CancellationToken.None);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(((TagResponse)created!.Value!).Label, Is.EqualTo("Beta"));
    }

    [Test]
    public async Task WhenDeletingATagThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Delete(ProjectId, 999, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingATagFromADifferentProject_ThenNotFoundIsReturned()
    {
        var created = await _controller.Create(ProjectId, new CreateTagRequest("Beta", "#FF0000"), CancellationToken.None);
        var tagId = ((TagResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Delete(999, tagId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingATagInTheCorrectProject_ThenNoContentIsReturnedAndTheTagIsRemoved()
    {
        var created = await _controller.Create(ProjectId, new CreateTagRequest("Beta", "#FF0000"), CancellationToken.None);
        var tagId = ((TagResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Delete(ProjectId, tagId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_tags.Any(t => t.Id == tagId), Is.False);
    }
}
