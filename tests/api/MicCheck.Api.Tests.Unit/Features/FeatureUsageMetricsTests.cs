using System.Diagnostics.Metrics;
using MicCheck.Api.Features;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureUsageMetricsTests
{
    private FeatureUsageMetrics _metrics = null!;

    [SetUp]
    public void SetUp()
    {
        var meterFactory = new Mock<IMeterFactory>();
        meterFactory.Setup(f => f.Create(It.IsAny<MeterOptions>())).Returns(new Meter("test"));
        _metrics = new FeatureUsageMetrics(meterFactory.Object);
    }

    [TearDown]
    public void TearDown() => _metrics.Dispose();

    [Test]
    public void WhenRecordingEvaluations_ThenAccumulatorIncrementsByFeatureAndEnvironment()
    {
        _metrics.RecordEvaluation(environmentId: 1, featureId: 10, featureName: "dark_mode");
        _metrics.RecordEvaluation(environmentId: 1, featureId: 10, featureName: "dark_mode");
        _metrics.RecordEvaluation(environmentId: 1, featureId: 20, featureName: "beta");
        _metrics.RecordEvaluation(environmentId: 2, featureId: 10, featureName: "dark_mode");

        var drained = _metrics.DrainAccumulated();

        Assert.That(drained, Has.Count.EqualTo(3));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.That(drained[new FeatureUsageBucketKey(1, 10, "dark_mode", today)], Is.EqualTo(2));
        Assert.That(drained[new FeatureUsageBucketKey(1, 20, "beta", today)], Is.EqualTo(1));
        Assert.That(drained[new FeatureUsageBucketKey(2, 10, "dark_mode", today)], Is.EqualTo(1));
    }

    [Test]
    public void WhenDrainCalled_ThenAccumulatorIsResetToEmpty()
    {
        _metrics.RecordEvaluation(environmentId: 1, featureId: 10, featureName: "dark_mode");

        _metrics.DrainAccumulated();
        var secondDrain = _metrics.DrainAccumulated();

        Assert.That(secondDrain, Is.Empty);
    }

    [Test]
    public void WhenNothingRecorded_ThenDrainReturnsEmptyDictionary()
    {
        var drained = _metrics.DrainAccumulated();

        Assert.That(drained, Is.Empty);
    }

    [Test]
    public void WhenRecordingConcurrently_ThenCountsAreAccurateAndNoDataLost()
    {
        const int threads = 8;
        const int recordsPerThread = 100;

        var tasks = Enumerable.Range(0, threads).Select(_ =>
            Task.Run(() =>
            {
                for (var i = 0; i < recordsPerThread; i++)
                    _metrics.RecordEvaluation(environmentId: 1, featureId: 1, featureName: "concurrent_flag");
            })).ToArray();

        Task.WaitAll(tasks);

        var drained = _metrics.DrainAccumulated();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.That(drained[new FeatureUsageBucketKey(1, 1, "concurrent_flag", today)], Is.EqualTo(threads * recordsPerThread));
    }
}
