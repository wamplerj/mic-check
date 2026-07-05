using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Segments;
using MicCheck.Api.Tests.Unit.TestSupport;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureSegmentServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Segment> _segments = null!;
    private List<FeatureSegment> _featureSegments = null!;
    private List<FeatureState> _featureStates = null!;
    private FeatureSegmentService _service = null!;
    private const int FeatureId = 1;
    private const int EnvironmentId = 1;
    private const int SegmentId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _segments = [new Segment { Id = SegmentId, Name = "Premium Users", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSetWithGeneratedIds(c => c.Segments, _segments);
        _featureSegments = [];
        _db.SetupDbSetWithGeneratedIds(c => c.FeatureSegments, _featureSegments);
        _featureStates = [];
        _db.SetupDbSetWithGeneratedIds(c => c.FeatureStates, _featureStates);

        _service = new FeatureSegmentService(_db.Object);
    }

    [Test]
    public async Task WhenCreatingAFeatureSegment_ThenItIsPersistedWithAMatchingFeatureState()
    {
        var response = await _service.CreateAsync(FeatureId, EnvironmentId, SegmentId, priority: 1, enabled: true, value: "on");

        Assert.That(response.SegmentName, Is.EqualTo("Premium Users"));
        Assert.That(response.Enabled, Is.True);
        Assert.That(_featureSegments, Has.Count.EqualTo(1));
        Assert.That(_featureStates.Single().FeatureSegmentId, Is.EqualTo(response.Id));
    }

    [Test]
    public void WhenCreatingAFeatureSegmentForASegmentThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateAsync(FeatureId, EnvironmentId, 999, priority: 1, enabled: true, value: null));
    }

    [Test]
    public async Task WhenListingFeatureSegments_ThenTheyAreOrderedByPriorityAndIncludeTheSegmentNameAndState()
    {
        await _service.CreateAsync(FeatureId, EnvironmentId, SegmentId, priority: 2, enabled: true, value: "second");
        _segments.Add(new Segment { Id = 2, Name = "Beta Users", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow });
        await _service.CreateAsync(FeatureId, EnvironmentId, 2, priority: 1, enabled: false, value: "first");

        var results = await _service.ListByFeatureAsync(FeatureId, EnvironmentId);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].Priority, Is.EqualTo(1));
        Assert.That(results[0].SegmentName, Is.EqualTo("Beta Users"));
        Assert.That(results[1].Priority, Is.EqualTo(2));
    }

    [Test]
    public async Task WhenListingFeatureSegmentsForADifferentEnvironment_ThenNoneAreReturned()
    {
        await _service.CreateAsync(FeatureId, EnvironmentId, SegmentId, priority: 1, enabled: true, value: null);

        var results = await _service.ListByFeatureAsync(FeatureId, 999);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task WhenUpdatingAFeatureSegmentThatDoesNotExist_ThenNullIsReturned()
    {
        var result = await _service.UpdateAsync(999, priority: 1, enabled: true, value: null);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task WhenUpdatingAFeatureSegment_ThenTheExistingStateIsUpdated()
    {
        var created = await _service.CreateAsync(FeatureId, EnvironmentId, SegmentId, priority: 1, enabled: false, value: "off");

        var updated = await _service.UpdateAsync(created.Id, priority: 5, enabled: true, value: "on");

        Assert.That(updated!.Priority, Is.EqualTo(5));
        Assert.That(updated.Enabled, Is.True);
        Assert.That(updated.Value, Is.EqualTo("on"));
        Assert.That(_featureStates, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenUpdatingAFeatureSegmentThatHasNoExistingState_ThenANewStateIsCreated()
    {
        var featureSegment = new FeatureSegment { Id = 1, FeatureId = FeatureId, SegmentId = SegmentId, EnvironmentId = EnvironmentId, Priority = 1 };
        _featureSegments.Add(featureSegment);

        var updated = await _service.UpdateAsync(featureSegment.Id, priority: 3, enabled: true, value: "on");

        Assert.That(updated!.Enabled, Is.True);
        Assert.That(_featureStates, Has.Count.EqualTo(1));
        Assert.That(_featureStates[0].FeatureSegmentId, Is.EqualTo(featureSegment.Id));
    }

    [Test]
    public async Task WhenDeletingAFeatureSegmentThatDoesNotExist_ThenFalseIsReturned()
    {
        var result = await _service.DeleteAsync(999);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task WhenDeletingAFeatureSegment_ThenTrueIsReturnedAndItIsRemoved()
    {
        var created = await _service.CreateAsync(FeatureId, EnvironmentId, SegmentId, priority: 1, enabled: true, value: null);

        var result = await _service.DeleteAsync(created.Id);

        Assert.That(result, Is.True);
        Assert.That(_featureSegments.Any(fsg => fsg.Id == created.Id), Is.False);
    }
}
