using MicCheck.Api.Data;
using MicCheck.Api.Identities;
using MicCheck.Api.Segments;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Features;

public class FeatureEvaluationService(
    MicCheckDbContext db,
    SegmentEvaluator segmentEvaluator,
    FlagCache flagCache)
{
    public async Task<IReadOnlyList<FeatureStateResult>> EvaluateForEnvironmentAsync(
        int environmentId, CancellationToken ct = default)
    {
        var cached = flagCache.Get(environmentId);
        if (cached is not null)
            return cached;

        var results = await (
            from fs in db.FeatureStates
            join f in db.Features on fs.FeatureId equals f.Id
            where fs.EnvironmentId == environmentId
               && fs.IdentityId == null
               && fs.FeatureSegmentId == null
            select new FeatureStateResult
            {
                Feature = f,
                Enabled = fs.Enabled,
                Value = fs.Value
            }
        ).ToListAsync(ct);

        flagCache.Set(environmentId, results);
        return results;
    }

    public async Task<IReadOnlyList<FeatureStateResult>> EvaluateForIdentityAsync(
        int environmentId,
        string identifier,
        IReadOnlyList<TraitInput>? traits = null,
        CancellationToken ct = default)
    {
        var environment = await db.Environments.FindAsync([environmentId], ct);
        if (environment is null)
            return [];

        var identity = await new IdentityResolutionService(db)
            .ResolveAsync(environmentId, identifier, traits, ct);

        var features = await db.Features
            .Where(f => f.ProjectId == environment.ProjectId)
            .ToListAsync(ct);

        var featureIds = features.Select(f => f.Id).ToList();

        var allFeatureStates = await db.FeatureStates
            .Where(fs => fs.EnvironmentId == environmentId && featureIds.Contains(fs.FeatureId))
            .ToListAsync(ct);

        var featureSegments = await db.FeatureSegments
            .Where(fsg => fsg.EnvironmentId == environmentId && featureIds.Contains(fsg.FeatureId))
            .OrderBy(fsg => fsg.Priority)
            .ToListAsync(ct);

        var segmentIds = featureSegments.Select(fsg => fsg.SegmentId).Distinct().ToList();
        var segments = await db.Segments
            .Include(s => s.Rules)
                .ThenInclude(r => r.Conditions)
            .Include(s => s.Rules)
                .ThenInclude(r => r.ChildRules)
                    .ThenInclude(cr => cr.Conditions)
            .Where(s => segmentIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, ct);

        var identityTraits = identity.Traits.ToList();
        var results = new List<FeatureStateResult>();

        foreach (var feature in features)
        {
            // 1. Identity-level override
            var identityOverride = allFeatureStates.FirstOrDefault(
                fs => fs.FeatureId == feature.Id && fs.IdentityId == identity.Id);
            if (identityOverride is not null)
            {
                results.Add(new FeatureStateResult
                {
                    Feature = feature,
                    Enabled = identityOverride.Enabled,
                    Value = identityOverride.Value
                });
                continue;
            }

            // 2. Segment overrides (highest priority segment that matches)
            var segmentOverrideFound = false;
            foreach (var fsg in featureSegments.Where(fsg => fsg.FeatureId == feature.Id))
            {
                if (!segments.TryGetValue(fsg.SegmentId, out var segment))
                    continue;
                if (!segmentEvaluator.Evaluate(segment, identityTraits, identity.Identifier))
                    continue;

                var segmentState = allFeatureStates.FirstOrDefault(
                    fs => fs.FeatureId == feature.Id && fs.FeatureSegmentId == fsg.Id);
                if (segmentState is null)
                    continue;

                results.Add(new FeatureStateResult
                {
                    Feature = feature,
                    Enabled = segmentState.Enabled,
                    Value = segmentState.Value
                });
                segmentOverrideFound = true;
                break;
            }
            if (segmentOverrideFound)
                continue;

            // 3. Environment default
            var envDefault = allFeatureStates.FirstOrDefault(
                fs => fs.FeatureId == feature.Id
                   && fs.IdentityId == null
                   && fs.FeatureSegmentId == null);
            if (envDefault is not null)
            {
                results.Add(new FeatureStateResult
                {
                    Feature = feature,
                    Enabled = envDefault.Enabled,
                    Value = envDefault.Value
                });
            }
        }

        return results;
    }
}
