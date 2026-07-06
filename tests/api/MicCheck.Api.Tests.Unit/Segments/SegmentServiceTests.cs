using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Segments;
using MicCheck.Api.Tests.Unit.TestSupport;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Segments;

[TestFixture]
public class SegmentServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Segment> _segments = null!;
    private List<SegmentRule> _segmentRules = null!;
    private List<SegmentCondition> _segmentConditions = null!;
    private SegmentService _service = null!;
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

        _service = new SegmentService(_db.Object, auditService.Object);
    }

    private static SegmentRuleDefinition SingleConditionRule(
        string property = "plan",
        SegmentConditionOperator op = SegmentConditionOperator.Equal,
        string value = "premium") =>
        new(SegmentRuleType.All,
            [new SegmentConditionDefinition(property, op, value)]);

    [Test]
    public async Task WhenCreatingASegment_ThenSegmentAndRulesArePersisted()
    {
        var rules = new[] { SingleConditionRule() };
        var segment = await _service.CreateAsync(ProjectId, "Premium Users", rules);

        var persistedRules = _segmentRules.Where(r => r.SegmentId == segment.Id).ToList();

        Assert.That(segment.Name, Is.EqualTo("Premium Users"));
        Assert.That(persistedRules, Has.Count.EqualTo(1));
        Assert.That(persistedRules[0].Conditions, Has.Count.EqualTo(1));
    }

    [Test]
    public void WhenProjectHas100Segments_ThenCreatingAnotherThrowsDomainException()
    {
        for (var i = 0; i < 100; i++)
        {
            _segments.Add(new Segment
            {
                Id = i + 1,
                Name = $"segment_{i}",
                ProjectId = ProjectId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(ProjectId, "overflow_segment", [SingleConditionRule()]));
    }

    [Test]
    public void WhenSegmentHasMoreThan100Conditions_ThenCreatingThrowsDomainException()
    {
        var conditions = Enumerable.Range(0, 101)
            .Select(i => new SegmentConditionDefinition($"prop_{i}", SegmentConditionOperator.Equal, "val"))
            .ToList();
        var rules = new[] { new SegmentRuleDefinition(SegmentRuleType.All, conditions) };

        Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(ProjectId, "Too Many Conditions", rules));
    }

    [Test]
    public async Task WhenUpdatingASegment_ThenOldRulesAreReplacedWithNewRules()
    {
        var segment = await _service.CreateAsync(ProjectId, "Segment", [SingleConditionRule("plan")]);

        var newRules = new[] { SingleConditionRule("country") };
        var updated = await _service.UpdateAsync(segment.Id, "Updated Segment", newRules);

        var persistedRules = _segmentRules.Where(r => r.SegmentId == updated.Id).ToList();

        Assert.That(updated.Name, Is.EqualTo("Updated Segment"));
        Assert.That(persistedRules, Has.Count.EqualTo(1));
        Assert.That(persistedRules[0].Conditions.First().Property, Is.EqualTo("country"));
    }

    [Test]
    public async Task WhenDeletingASegment_ThenItIsRemovedFromTheDatabase()
    {
        var segment = await _service.CreateAsync(ProjectId, "To Delete", [SingleConditionRule()]);

        await _service.DeleteAsync(segment.Id);

        var found = _segments.FirstOrDefault(s => s.Id == segment.Id);
        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task WhenListingSegments_ThenAllProjectSegmentsAreReturned()
    {
        await _service.CreateAsync(ProjectId, "Segment A", [SingleConditionRule()]);
        await _service.CreateAsync(ProjectId, "Segment B", [SingleConditionRule()]);

        var segments = await _service.ListByProjectAsync(ProjectId);

        Assert.That(segments, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task WhenFindingASegmentByIdThatExists_ThenTheSegmentIsReturned()
    {
        var segment = await _service.CreateAsync(ProjectId, "Segment", [SingleConditionRule()]);

        var found = await _service.FindByIdAsync(segment.Id);

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Id, Is.EqualTo(segment.Id));
    }

    [Test]
    public async Task WhenFindingASegmentByIdThatDoesNotExist_ThenNullIsReturned()
    {
        var found = await _service.FindByIdAsync(999);

        Assert.That(found, Is.Null);
    }

    [Test]
    public void WhenUpdatingASegmentThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateAsync(999, "Renamed", [SingleConditionRule()]));
    }

    [Test]
    public void WhenDeletingASegmentThatDoesNotExist_ThenNoExceptionIsThrown()
    {
        Assert.DoesNotThrowAsync(() => _service.DeleteAsync(999));
    }

    [Test]
    public async Task WhenCreatingASegmentWithNestedChildRules_ThenChildRulesArePersistedWithTheParentAsTheirParent()
    {
        var childRule = new SegmentRuleDefinition(SegmentRuleType.Any, [new SegmentConditionDefinition("country", SegmentConditionOperator.Equal, "US")]);
        var parentRule = new SegmentRuleDefinition(SegmentRuleType.All, [new SegmentConditionDefinition("plan", SegmentConditionOperator.Equal, "premium")], [childRule]);

        var segment = await _service.CreateAsync(ProjectId, "Nested", [parentRule]);

        var persistedRules = _segmentRules.Where(r => r.SegmentId == segment.Id).ToList();
        var parent = persistedRules.Single(r => r.ParentRuleId == null);
        var child = persistedRules.Single(r => r.ParentRuleId == parent.Id);

        Assert.That(child.Conditions.Single().Property, Is.EqualTo("country"));
    }
}
