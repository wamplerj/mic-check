using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Organizations;

public class OrganizationService(MicCheckDbContext db, AuditService auditService)
{
    public async Task<IReadOnlyList<Organization>> ListForUserAsync(int userId, CancellationToken ct = default)
    {
        return await db.Organizations
            .Where(o => o.Members.Any(m => m.UserId == userId))
            .ToListAsync(ct);
    }

    public async Task<Organization?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Organization> CreateAsync(string name, int creatorUserId, CancellationToken ct = default)
    {
        var org = new Organization
        {
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };
        org.Members.Add(new OrganizationUser
        {
            UserId = creatorUserId,
            Role = OrganizationRole.Admin
        });
        db.Organizations.Add(org);
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync("Organization", org.Id.ToString(), "created", org.Id, ct: ct);

        return org;
    }

    public async Task<Organization> UpdateAsync(int id, string name, CancellationToken ct = default)
    {
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new KeyNotFoundException($"Organization {id} not found.");

        org.Name = name;
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync("Organization", org.Id.ToString(), "updated", org.Id, ct: ct);

        return org;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (org is null) return;

        db.Organizations.Remove(org);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OrganizationUser>> ListMembersAsync(int organizationId, CancellationToken ct = default)
    {
        return await db.OrganizationUsers
            .Where(m => m.OrganizationId == organizationId)
            .Include(m => m.User)
            .ToListAsync(ct);
    }

    public async Task InviteUserAsync(int organizationId, int userId, OrganizationRole role, CancellationToken ct = default)
    {
        var existing = await db.OrganizationUsers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, ct);

        if (existing is not null)
        {
            existing.Role = role;
        }
        else
        {
            db.OrganizationUsers.Add(new OrganizationUser
            {
                OrganizationId = organizationId,
                UserId = userId,
                Role = role
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(int organizationId, int userId, CancellationToken ct = default)
    {
        var member = await db.OrganizationUsers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, ct);

        if (member is null) return;

        db.OrganizationUsers.Remove(member);
        await db.SaveChangesAsync(ct);
    }
}
