using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Audit;

public class AuditLogQueryService(MicCheckDbContext db)
{
    public async Task<(int Total, IReadOnlyList<AuditLogResponse> Items)> ListByOrganizationAsync(
        int organizationId, AuditLogFilter filter, CancellationToken ct = default)
    {
        var query = db.AuditLogs.Where(l => l.OrganizationId == organizationId);
        return await ApplyFilterAndPageAsync(query, filter, ct);
    }

    public async Task<(int Total, IReadOnlyList<AuditLogResponse> Items)> ListByProjectAsync(
        int projectId, AuditLogFilter filter, CancellationToken ct = default)
    {
        var query = db.AuditLogs.Where(l => l.ProjectId == projectId);
        return await ApplyFilterAndPageAsync(query, filter, ct);
    }

    public async Task<(int Total, IReadOnlyList<AuditLogResponse> Items)> ListByEnvironmentAsync(
        int environmentId, AuditLogFilter filter, CancellationToken ct = default)
    {
        var query = db.AuditLogs.Where(l => l.EnvironmentId == environmentId);
        return await ApplyFilterAndPageAsync(query, filter, ct);
    }

    private async Task<(int Total, IReadOnlyList<AuditLogResponse> Items)> ApplyFilterAndPageAsync(
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
            .GroupJoin(
                db.Users,
                log => log.ActorUserId,
                user => (int?)user.Id,
                (log, users) => new { log, users })
            .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (x, user) => AuditLogResponse.From(
                    x.log,
                    user != null ? user.FirstName + " " + user.LastName : null))
            .ToListAsync(ct);

        return (total, items);
    }
}
