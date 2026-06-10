using MicCheck.Api.Data;
using MicCheck.Api.Segments;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Features;

public class FeatureSegmentService(MicCheckDbContext db)
{
    public async Task<IReadOnlyList<FeatureSegmentResponse>> ListByFeatureAsync(
        int featureId, int environmentId, CancellationToken ct = default)
    {
        var featureSegments = await db.FeatureSegments
            .Where(fsg => fsg.FeatureId == featureId && fsg.EnvironmentId == environmentId)
            .OrderBy(fsg => fsg.Priority)
            .ToListAsync(ct);

        var segmentIds = featureSegments.Select(fsg => fsg.SegmentId).Distinct().ToList();
        var segments = await db.Segments
            .Where(s => segmentIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, ct);

        var states = await db.FeatureStates
            .Where(fs => fs.FeatureId == featureId && fs.EnvironmentId == environmentId && fs.FeatureSegmentId != null)
            .ToListAsync(ct);
        var stateBySegment = states.ToDictionary(fs => fs.FeatureSegmentId!.Value);

        return featureSegments.Select(fsg =>
        {
            var segmentName = segments.TryGetValue(fsg.SegmentId, out var seg) ? seg.Name : "Unknown";
            stateBySegment.TryGetValue(fsg.Id, out var state);
            return new FeatureSegmentResponse(
                fsg.Id, fsg.FeatureId, fsg.SegmentId, segmentName,
                fsg.EnvironmentId, fsg.Priority,
                state?.Enabled, state?.Value);
        }).ToList();
    }

    public async Task<FeatureSegmentResponse> CreateAsync(
        int featureId, int environmentId, int segmentId, int priority, bool enabled, string? value,
        CancellationToken ct = default)
    {
        var segment = await db.Segments.FirstOrDefaultAsync(s => s.Id == segmentId, ct)
            ?? throw new KeyNotFoundException($"Segment {segmentId} not found.");

        var featureSegment = new FeatureSegment
        {
            FeatureId = featureId,
            SegmentId = segmentId,
            EnvironmentId = environmentId,
            Priority = priority
        };
        db.FeatureSegments.Add(featureSegment);
        await db.SaveChangesAsync(ct);

        var state = new FeatureState
        {
            FeatureId = featureId,
            EnvironmentId = environmentId,
            FeatureSegmentId = featureSegment.Id,
            Enabled = enabled,
            Value = value,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.FeatureStates.Add(state);
        await db.SaveChangesAsync(ct);

        return new FeatureSegmentResponse(
            featureSegment.Id, featureId, segmentId, segment.Name,
            environmentId, priority, enabled, value);
    }

    public async Task<FeatureSegmentResponse?> UpdateAsync(
        int id, int priority, bool enabled, string? value, CancellationToken ct = default)
    {
        var featureSegment = await db.FeatureSegments.FirstOrDefaultAsync(fsg => fsg.Id == id, ct);
        if (featureSegment is null) return null;

        var segment = await db.Segments.FirstOrDefaultAsync(s => s.Id == featureSegment.SegmentId, ct);

        featureSegment.Priority = priority;

        var state = await db.FeatureStates
            .FirstOrDefaultAsync(fs => fs.FeatureSegmentId == id, ct);

        if (state is not null)
        {
            state.Enabled = enabled;
            state.Value = value;
            state.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            state = new FeatureState
            {
                FeatureId = featureSegment.FeatureId,
                EnvironmentId = featureSegment.EnvironmentId,
                FeatureSegmentId = featureSegment.Id,
                Enabled = enabled,
                Value = value,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.FeatureStates.Add(state);
        }

        await db.SaveChangesAsync(ct);

        return new FeatureSegmentResponse(
            featureSegment.Id, featureSegment.FeatureId, featureSegment.SegmentId,
            segment?.Name ?? "Unknown", featureSegment.EnvironmentId,
            priority, enabled, value);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var featureSegment = await db.FeatureSegments.FirstOrDefaultAsync(fsg => fsg.Id == id, ct);
        if (featureSegment is null) return false;

        db.FeatureSegments.Remove(featureSegment);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
