using MicCheck.Api.Common.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace MicCheck.Api.Features;

[ApiController]
[Route("api/v1/environments/{environmentId}/usage")]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class FeatureUsageController(FeatureUsageQueryService queryService, IMemoryCache cache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardUsageResponse>> GetDashboardUsage(
        int environmentId,
        [FromQuery] int days = 14,
        CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 90);
        var cacheKey = $"usage-dashboard:{environmentId}:{days}";

        if (cache.TryGetValue(cacheKey, out DashboardUsageResponse? cached))
            return Ok(cached);

        var result = await queryService.GetDashboardUsageAsync(environmentId, days, ct);

        cache.Set(cacheKey, result, TimeSpan.FromSeconds(180));
        return Ok(result);
    }
}
