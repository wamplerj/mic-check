using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace MicCheck.Api.Features;

public class FeatureUsageMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _evaluationCounter;
    private ConcurrentDictionary<FeatureUsageBucketKey, long> _accumulator = new();

    public FeatureUsageMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("MicCheck.FeatureUsage");
        _evaluationCounter = _meter.CreateCounter<long>("miccheck.feature.evaluations", description: "Number of feature flag evaluations");
    }

    public void RecordEvaluation(int environmentId, int featureId, string featureName)
    {
        _evaluationCounter.Add(1,
            new KeyValuePair<string, object?>("environment.id", environmentId),
            new KeyValuePair<string, object?>("feature.id", featureId));

        var key = new FeatureUsageBucketKey(environmentId, featureId, featureName, DateOnly.FromDateTime(DateTime.UtcNow));
        _accumulator.AddOrUpdate(key, 1L, (_, existing) => existing + 1);
    }

    public IReadOnlyDictionary<FeatureUsageBucketKey, long> DrainAccumulated()
    {
        var drained = Interlocked.Exchange(ref _accumulator, new ConcurrentDictionary<FeatureUsageBucketKey, long>());
        return drained;
    }

    public void Dispose() => _meter.Dispose();
}
