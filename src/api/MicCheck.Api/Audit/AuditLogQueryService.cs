using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Audit;

public class AuditLogQueryService(MicCheckDbContext db)
{
    public async Task<(int Total, IReadOnlyList<AuditLog> Items)> ListByOrganizationAsync(
        int organizationId, AuditLogFilter filter, CancellationToken ct = default)
    {
        var query = db.AuditLogs.Where(l => l.OrganizationId == organizationId);
        return await ApplyFilterAndPageAsync(query, filter, ct);
    }

    public async Task<(int Total, IReadOnlyList<AuditLog> Items)> ListByProjectAsync(
        int projectId, AuditLogFilter filter, CancellationToken ct = default)
    {
        var query = db.AuditLogs.Where(l => l.ProjectId == projectId);
        return await ApplyFilterAndPageAsync(query, filter, ct);
    }

    public async Task<(int Total, IReadOnlyList<AuditLog> Items)> ListByEnvironmentAsync(
        int environmentId, AuditLogFilter filter, CancellationToken ct = default)
    {
        var query = db.AuditLogs.Where(l => l.EnvironmentId == environmentId);
        return await ApplyFilterAndPageAsync(query, filter, ct);
    }

    private static async Task<(int Total, IReadOnlyList<AuditLog> Items)> ApplyFilterAndPageAsync(
        IQueryable<AuditLog> query, AuditLogFilter filter, CancellationToken ct)
    {
        if (filter.From.HasValue)
            query = query.Where(l => l.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(l => l.CreatedAt <= filter.To.Value);
        if (!string.IsNullOrEmpty(filter.ResourceType))
            query = query.Where(l => l.ResourceType == filter.ResourceType);
        if (!string.IsNullOrEmpty(filter.Action))
            query = query.Where(l => l.Action == filter.Action);
        if (filter.ProjectId.HasValue)
            query = query.Where(l => l.ProjectId == filter.ProjectId);
        if (filter.EnvironmentId.HasValue)
            query = query.Where(l => l.EnvironmentId == filter.EnvironmentId);
        if (filter.ActorUserId.HasValue)
            query = query.Where(l => l.ActorUserId == filter.ActorUserId);

        var total = await query.CountAsync(ct);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((filter.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (total, items);
    }
}
