using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using MicCheck.Api.Segments;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Segments;

[TestFixture]
public class SegmentServiceTests
{
    private MicCheckDbContext _db = null!;
    private SegmentService _service = null!;
    private const int OrganizationId = 1;
    private const int ProjectId = 1;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new MicCheckDbContext(options);

        var auditService = new Mock<AuditService>(_db, null!);
        auditService.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new SegmentService(_db, auditService.Object);

        SeedBaseData();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private void SeedBaseData()
    {
        _db.Organizations.Add(new MicCheck.Api.Organizations.Organization
        {
            Id = OrganizationId,
            Name = "Test Org",
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.Projects.Add(new MicCheck.Api.Projects.Project
        {
            Id = ProjectId,
            Name = "Test Project",
            OrganizationId = OrganizationId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();
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

        var loaded = await _db.Segments
            .Include(s => s.Rules)
            .ThenInclude(r => r.Conditions)
            .FirstAsync(s => s.Id == segment.Id);

        Assert.That(loaded.Name, Is.EqualTo("Premium Users"));
        Assert.That(loaded.Rules, Has.Count.EqualTo(1));
        Assert.That(loaded.Rules.First().Conditions, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenProjectHas100Segments_ThenCreatingAnotherThrowsDomainException()
    {
        for (var i = 0; i < 100; i++)
        {
            _db.Segments.Add(new Segment
            {
                Name = $"segment_{i}",
                ProjectId = ProjectId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        _db.SaveChanges();

        Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(ProjectId, "overflow_segment", [SingleConditionRule()]));
    }

    [Test]
    public async Task WhenSegmentHasMoreThan100Conditions_ThenCreatingThrowsDomainException()
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

        var loaded = await _db.Segments
            .Include(s => s.Rules)
            .ThenInclude(r => r.Conditions)
            .FirstAsync(s => s.Id == updated.Id);

        Assert.That(loaded.Name, Is.EqualTo("Updated Segment"));
        Assert.That(loaded.Rules, Has.Count.EqualTo(1));
        Assert.That(loaded.Rules.First().Conditions.First().Property, Is.EqualTo("country"));
    }

    [Test]
    public async Task WhenDeletingASegment_ThenItIsRemovedFromTheDatabase()
    {
        var segment = await _service.CreateAsync(ProjectId, "To Delete", [SingleConditionRule()]);

        await _service.DeleteAsync(segment.Id);

        var found = await _db.Segments.FirstOrDefaultAsync(s => s.Id == segment.Id);
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
}
