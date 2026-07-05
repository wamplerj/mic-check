using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Segments;

public record SegmentConditionDefinition(string Property, SegmentConditionOperator Operator, string Value);

public record SegmentRuleDefinition(SegmentRuleType Type, IReadOnlyList<SegmentConditionDefinition> Conditions, IReadOnlyList<SegmentRuleDefinition>? ChildRules = null);

public class SegmentService(IMicCheckDbContext db, AuditService auditService)
{
    private const int MaxSegmentsPerProject = 100;
    private const int MaxConditionsPerSegment = 100;

    public async Task<IReadOnlyList<Segment>> ListByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await db.Segments
            .Include(s => s.Rules)
            .ThenInclude(r => r.Conditions)
            .Where(s => s.ProjectId == projectId)
            .ToListAsync(ct);
    }

    public async Task<Segment?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.Segments
            .Include(s => s.Rules)
            .ThenInclude(r => r.Conditions)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Segment> CreateAsync(int projectId, string name, IReadOnlyList<SegmentRuleDefinition> rules, CancellationToken ct = default)
    {
        var count = await db.Segments.CountAsync(s => s.ProjectId == projectId, ct);
        if (count >= MaxSegmentsPerProject)
            throw new DomainException($"Project has reached the maximum of {MaxSegmentsPerProject} segments.");

        var totalConditions = rules.Sum(r => CountConditions(r));
        if (totalConditions > MaxConditionsPerSegment)
            throw new DomainException($"Segment cannot have more than {MaxConditionsPerSegment} conditions.");

        var segment = new Segment
        {
            Name = name,
            ProjectId = projectId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Segments.Add(segment);
        await db.SaveChangesAsync(ct);

        foreach (var ruleDef in rules)
        {
            AddRule(segment.Id, ruleDef, parentRuleId: null);
        }

        await db.SaveChangesAsync(ct);

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project is not null)
            await auditService.LogAsync("Segment", segment.Id.ToString(), "created",
                project.OrganizationId, projectId, ct: ct);

        return segment;
    }

    public async Task<Segment> UpdateAsync(int id, string name, IReadOnlyList<SegmentRuleDefinition> rules, CancellationToken ct = default)
    {
        var segment = await db.Segments
            .Include(s => s.Rules)
            .ThenInclude(r => r.Conditions)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException($"Segment {id} not found.");

        var totalConditions = rules.Sum(r => CountConditions(r));
        if (totalConditions > MaxConditionsPerSegment)
            throw new DomainException($"Segment cannot have more than {MaxConditionsPerSegment} conditions.");

        var existingRules = await db.SegmentRules
            .Where(r => r.SegmentId == id)
            .ToListAsync(ct);
        var existingConditions = await db.SegmentConditions
            .Where(c => existingRules.Select(r => r.Id).Contains(c.RuleId))
            .ToListAsync(ct);

        db.SegmentConditions.RemoveRange(existingConditions);
        db.SegmentRules.RemoveRange(existingRules);
        await db.SaveChangesAsync(ct);

        segment.Name = name;

        foreach (var ruleDef in rules)
        {
            AddRule(segment.Id, ruleDef, parentRuleId: null);
        }

        await db.SaveChangesAsync(ct);

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == segment.ProjectId, ct);
        if (project is not null)
            await auditService.LogAsync("Segment", segment.Id.ToString(), "updated",
                project.OrganizationId, segment.ProjectId, ct: ct);

        return segment;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var segment = await db.Segments.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (segment is null) return;

        db.Segments.Remove(segment);
        await db.SaveChangesAsync(ct);
    }

    private void AddRule(int segmentId, SegmentRuleDefinition def, int? parentRuleId)
    {
        var rule = new SegmentRule
        {
            SegmentId = segmentId,
            ParentRuleId = parentRuleId,
            Type = def.Type
        };
        foreach (var cond in def.Conditions)
        {
            rule.Conditions.Add(new SegmentCondition
            {
                Property = cond.Property,
                Operator = cond.Operator,
                Value = cond.Value
            });
        }
        db.SegmentRules.Add(rule);

        if (def.ChildRules is not null)
        {
            foreach (var child in def.ChildRules)
            {
                AddRule(segmentId, child, rule.Id);
            }
        }
    }

    private static int CountConditions(SegmentRuleDefinition rule)
    {
        var count = rule.Conditions.Count;
        if (rule.ChildRules is not null)
            count += rule.ChildRules.Sum(CountConditions);
        return count;
    }
}
