using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeaturesControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Feature> _features = null!;
    private List<Tag> _tags = null!;
    private FeaturesController _controller = null!;
    private const int OrganizationId = 1;
    private const int ProjectId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _db.SetupDbSet(c => c.Organizations, [
            new Organization { Id = OrganizationId, Name = "Test Org", CreatedAt = DateTimeOffset.UtcNow }
        ]);
        _db.SetupDbSet(c => c.Projects, [
            new Project { Id = ProjectId, Name = "Test Project", OrganizationId = OrganizationId, CreatedAt = DateTimeOffset.UtcNow }
        ]);
        _db.SetupDbSet(c => c.Environments, new List<AppEnvironment>());
        _features = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Features, _features);
        _db.SetupDbSet(c => c.FeatureStates, new List<FeatureState>());
        _tags = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Tags, _tags);

        var webhookQueue = new MicCheck.Api.Webhooks.WebhookQueue();
        var auditService = new Mock<IAuditService>();
        auditService.Setup(a => a.RecordAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new FeatureService(_db.Object, auditService.Object, webhookQueue);
        _controller = new FeaturesController(service);
    }

    private static CreateFeatureRequest ValidRequest(string name = "dark_mode") =>
        new(name, FeatureType.Standard, null, null);

    [Test]
    public async Task WhenListingFeaturesForAProject_ThenAllFeaturesAreReturnedInAPage()
    {
        await _controller.Create(ProjectId, ValidRequest("feature_a"), CancellationToken.None);
        await _controller.Create(ProjectId, ValidRequest("feature_b"), CancellationToken.None);

        var result = await _controller.List(ProjectId);

        var ok = result.Result as OkObjectResult;
        var page = (PaginatedResponse<FeatureResponse>)ok!.Value!;
        Assert.That(page.Count, Is.EqualTo(2));
        Assert.That(page.Results, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task WhenListingFeaturesWithAPageSizeAboveTheMaximum_ThenThePageSizeIsClampedTo100()
    {
        for (var i = 0; i < 3; i++)
            await _controller.Create(ProjectId, ValidRequest($"feature_{i}"), CancellationToken.None);

        var result = await _controller.List(ProjectId, page: 1, pageSize: 1000);

        var ok = result.Result as OkObjectResult;
        var page = (PaginatedResponse<FeatureResponse>)ok!.Value!;
        Assert.That(page.Results, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task WhenCreatingAFeatureWithValidData_ThenACreatedResultWithTheFeatureIsReturned()
    {
        var result = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(((FeatureResponse)created!.Value!).Name, Is.EqualTo("dark_mode"));
    }

    [Test]
    public async Task WhenCreatingAFeatureThatExceedsTheProjectLimit_ThenBadRequestIsReturned()
    {
        for (var i = 0; i < 400; i++)
            _features.Add(new Feature { Id = i + 1, Name = $"feature_{i}", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.Create(ProjectId, ValidRequest("overflow"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task WhenGettingAFeatureByIdInTheCorrectProject_ThenTheFeatureIsReturned()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var featureId = ((FeatureResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.GetById(ProjectId, featureId, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(((FeatureResponse)ok!.Value!).Id, Is.EqualTo(featureId));
    }

    [Test]
    public async Task WhenGettingAFeatureThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetById(ProjectId, 999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingAFeatureThatBelongsToADifferentProject_ThenNotFoundIsReturned()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var featureId = ((FeatureResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.GetById(999, featureId, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAFeatureThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Update(ProjectId, 999, new UpdateFeatureRequest("renamed", null), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAFeatureInTheCorrectProject_ThenTheUpdatedFeatureIsReturned()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var featureId = ((FeatureResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Update(ProjectId, featureId, new UpdateFeatureRequest("renamed", "new desc"), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(((FeatureResponse)ok!.Value!).Name, Is.EqualTo("renamed"));
    }

    [Test]
    public async Task WhenPatchingAFeatureThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Patch(ProjectId, 999, new PatchFeatureRequest("renamed", null, null), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenPatchingOnlyTheNameField_ThenDescriptionAndDefaultEnabledAreUnchanged()
    {
        var created = await _controller.Create(ProjectId, new CreateFeatureRequest("original", FeatureType.Standard, null, "original desc"), CancellationToken.None);
        var featureId = ((FeatureResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Patch(ProjectId, featureId, new PatchFeatureRequest("renamed", null, null), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var response = (FeatureResponse)ok!.Value!;
        Assert.That(response.Name, Is.EqualTo("renamed"));
        Assert.That(response.Description, Is.EqualTo("original desc"));
        Assert.That(response.DefaultEnabled, Is.False);
    }

    [Test]
    public async Task WhenPatchingTheDefaultEnabledField_ThenItIsUpdated()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var featureId = ((FeatureResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Patch(ProjectId, featureId, new PatchFeatureRequest(null, null, true), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(((FeatureResponse)ok!.Value!).DefaultEnabled, Is.True);
    }

    [Test]
    public async Task WhenDeletingAFeatureThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Delete(ProjectId, 999, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAFeatureInTheCorrectProject_ThenNoContentIsReturnedAndTheFeatureIsRemoved()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var featureId = ((FeatureResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Delete(ProjectId, featureId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_features.Any(f => f.Id == featureId), Is.False);
    }

    [Test]
    public async Task WhenAssigningATagThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var featureId = ((FeatureResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.AssignTag(ProjectId, featureId, 999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenAssigningATagFromAnotherProject_ThenBadRequestIsReturned()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var featureId = ((FeatureResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        var foreignTag = new Tag { Id = 1, Label = "beta", Color = "#FF0000", ProjectId = 999 };
        _tags.Add(foreignTag);

        var result = await _controller.AssignTag(ProjectId, featureId, foreignTag.Id, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task WhenAssigningAValidTag_ThenTheFeatureResponseIncludesTheTag()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var featureId = ((FeatureResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        var tag = new Tag { Id = 1, Label = "beta", Color = "#FF0000", ProjectId = ProjectId };
        _tags.Add(tag);

        var result = await _controller.AssignTag(ProjectId, featureId, tag.Id, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(((FeatureResponse)ok!.Value!).Tags.Select(t => t.Id), Does.Contain(tag.Id));
    }

    [Test]
    public async Task WhenFeatureForAssignTagDoesNotExistInTheProject_ThenNotFoundIsReturned()
    {
        var result = await _controller.AssignTag(ProjectId, 999, 1, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenRemovingTagFromFeatureThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.RemoveTag(ProjectId, 999, 1, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenRemovingAnAssignedTag_ThenTheFeatureResponseNoLongerIncludesIt()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var featureId = ((FeatureResponse)((CreatedAtActionResult)created.Result!).Value!).Id;
        var tag = new Tag { Id = 1, Label = "beta", Color = "#FF0000", ProjectId = ProjectId };
        _tags.Add(tag);
        await _controller.AssignTag(ProjectId, featureId, tag.Id, CancellationToken.None);

        var result = await _controller.RemoveTag(ProjectId, featureId, tag.Id, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(((FeatureResponse)ok!.Value!).Tags.Select(t => t.Id), Does.Not.Contain(tag.Id));
    }
}
