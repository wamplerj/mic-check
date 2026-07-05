using MicCheck.Api.Audit;
using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Projects;

public class ProjectService(IMicCheckDbContext db, AuditService auditService)
{
    public async Task<IReadOnlyList<Project>> ListByOrganizationAsync(int organizationId, CancellationToken ct = default)
    {
        return await db.Projects
            .Where(p => p.OrganizationId == organizationId)
            .ToListAsync(ct);
    }

    public async Task<Project?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Project> CreateAsync(int organizationId, string name, CancellationToken ct = default)
    {
        var project = new Project
        {
            Name = name,
            OrganizationId = organizationId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync("Project", project.Id.ToString(), "created", organizationId, project.Id, ct: ct);

        return project;
    }

    public async Task<Project> UpdateAsync(int id, string name, bool hideDisabledFlags, CancellationToken ct = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Project {id} not found.");

        project.Name = name;
        project.HideDisabledFlags = hideDisabledFlags;
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync("Project", project.Id.ToString(), "updated", project.OrganizationId, project.Id, ct: ct);

        return project;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return;

        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<UserProjectPermission>> ListUserPermissionsAsync(int projectId, CancellationToken ct = default)
    {
        return await db.UserProjectPermissions
            .Where(p => p.ProjectId == projectId)
            .ToListAsync(ct);
    }

    public async Task<UserProjectPermission> SetUserPermissionsAsync(
        int projectId, int userId, bool isAdmin, List<ProjectPermission> permissions, CancellationToken ct = default)
    {
        var existing = await db.UserProjectPermissions
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.UserId == userId, ct);

        if (existing is not null)
        {
            existing.IsAdmin = isAdmin;
            existing.Permissions = permissions;
        }
        else
        {
            existing = new UserProjectPermission
            {
                ProjectId = projectId,
                UserId = userId,
                IsAdmin = isAdmin,
                Permissions = permissions
            };
            db.UserProjectPermissions.Add(existing);
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task RemoveUserPermissionsAsync(int projectId, int userId, CancellationToken ct = default)
    {
        var perm = await db.UserProjectPermissions
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.UserId == userId, ct);

        if (perm is null) return;

        db.UserProjectPermissions.Remove(perm);
        await db.SaveChangesAsync(ct);
    }
}
