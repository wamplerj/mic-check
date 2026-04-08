using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Audit;

public class AuditLogQueryService(MicCheckDbContext db)
{
    public async Task<IReadOnlyList<AuditLog>> ListByOrganizationAsync(int organizationId, CancellationToken ct = default)
    {
        return await db.AuditLogs
            .Where(l => l.OrganizationId == organizationId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLog>> ListByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await db.AuditLogs
            .Where(l => l.ProjectId == projectId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLog>> ListByEnvironmentAsync(int environmentId, CancellationToken ct = default)
    {
        return await db.AuditLogs
            .Where(l => l.EnvironmentId == environmentId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
    }
}
