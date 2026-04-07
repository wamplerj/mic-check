using Microsoft.Extensions.Caching.Memory;

namespace MicCheck.Api.Features;

public class FlagCache(IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public IReadOnlyList<FeatureStateResult>? Get(int environmentId)
    {
        cache.TryGetValue(CacheKey(environmentId), out IReadOnlyList<FeatureStateResult>? flags);
        return flags;
    }

    public void Set(int environmentId, IReadOnlyList<FeatureStateResult> flags)
        => cache.Set(CacheKey(environmentId), flags, CacheDuration);

    public void Invalidate(int environmentId)
        => cache.Remove(CacheKey(environmentId));

    private static string CacheKey(int environmentId) => $"flags:{environmentId}";
}
