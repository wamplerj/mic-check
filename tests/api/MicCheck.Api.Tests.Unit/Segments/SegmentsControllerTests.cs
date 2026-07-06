using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Segments;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Segments;

[TestFixture]
public class SegmentsControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Segment> _segments = null!;
    private List<SegmentRule> _segmentRules = null!;
    private List<SegmentCondition> _segmentConditions = null!;
    private SegmentsController _controller = null!;
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
        _segments = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Segments, _segments);
        _segmentRules = [];
        _db.SetupDbSetWithGeneratedIds(c => c.SegmentRules, _segmentRules);
        _segmentConditions = [];
        _db.SetupDbSetWithGeneratedIds(c => c.SegmentConditions, _segmentConditions);

        var webhookQueue = new MicCheck.Api.Webhooks.WebhookQueue();
        var auditService = new Mock<IAuditService>();
        auditService.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new SegmentService(_db.Object, auditService.Object);
        _controller = new SegmentsController(service);
    }

    private static CreateSegmentRequest ValidRequest(string name = "Premium Users") => new(
        name,
        [new CreateSegmentRuleRequest("All", [new CreateSegmentConditionRequest("plan", "Equal", "premium")])]);

    [Test]
    public async Task WhenListingSegmentsForAProject_ThenAllSegmentsAreReturnedInAPage()
    {
        await _controller.Create(ProjectId, ValidRequest("Segment A"), CancellationToken.None);
        await _controller.Create(ProjectId, ValidRequest("Segment B"), CancellationToken.None);

        var result = await _controller.List(ProjectId);

        var ok = result.Result as OkObjectResult;
        var page = (PaginatedResponse<SegmentResponse>)ok!.Value!;
        Assert.That(page.Count, Is.EqualTo(2));
        Assert.That(page.Results, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task WhenListingSegmentsWithAPageSizeAboveTheMaximum_ThenThePageSizeIsClampedTo100()
    {
        for (var i = 0; i < 3; i++)
            await _controller.Create(ProjectId, ValidRequest($"Segment {i}"), CancellationToken.None);

        var result = await _controller.List(ProjectId, page: 1, pageSize: 1000);

        var ok = result.Result as OkObjectResult;
        var page = (PaginatedResponse<SegmentResponse>)ok!.Value!;
        Assert.That(page.Results, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task WhenCreatingASegmentWithValidData_ThenACreatedResultWithTheSegmentIsReturned()
    {
        var result = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(((SegmentResponse)created!.Value!).Name, Is.EqualTo("Premium Users"));
    }

    [Test]
    public async Task WhenCreatingASegmentThatExceedsTheProjectLimit_ThenBadRequestIsReturned()
    {
        for (var i = 0; i < 100; i++)
        {
            _segments.Add(new Segment { Id = i + 1, Name = $"segment_{i}", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow });
        }

        var result = await _controller.Create(ProjectId, ValidRequest("overflow"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task WhenGettingASegmentByIdInTheCorrectProject_ThenTheSegmentIsReturned()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var segmentId = ((SegmentResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.GetById(ProjectId, segmentId, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(((SegmentResponse)ok!.Value!).Id, Is.EqualTo(segmentId));
    }

    [Test]
    public async Task WhenGettingASegmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetById(ProjectId, 999, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingASegmentThatBelongsToADifferentProject_ThenNotFoundIsReturned()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var segmentId = ((SegmentResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.GetById(999, segmentId, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingASegmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Update(ProjectId, 999, ValidRequest(), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingASegmentInTheCorrectProject_ThenTheUpdatedSegmentIsReturned()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var segmentId = ((SegmentResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Update(ProjectId, segmentId, ValidRequest("Renamed"), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(((SegmentResponse)ok!.Value!).Name, Is.EqualTo("Renamed"));
    }

    [Test]
    public async Task WhenUpdatingASegmentWithTooManyConditions_ThenBadRequestIsReturned()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var segmentId = ((SegmentResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var conditions = Enumerable.Range(0, 101)
            .Select(i => new CreateSegmentConditionRequest($"prop_{i}", "Equal", "val"))
            .ToList();
        var overflowRequest = new CreateSegmentRequest("Too Many", [new CreateSegmentRuleRequest("All", conditions)]);

        var result = await _controller.Update(ProjectId, segmentId, overflowRequest, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task WhenDeletingASegmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Delete(ProjectId, 999, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingASegmentInTheCorrectProject_ThenNoContentIsReturnedAndTheSegmentIsRemoved()
    {
        var created = await _controller.Create(ProjectId, ValidRequest(), CancellationToken.None);
        var segmentId = ((SegmentResponse)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await _controller.Delete(ProjectId, segmentId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_segments.Any(s => s.Id == segmentId), Is.False);
    }
}
