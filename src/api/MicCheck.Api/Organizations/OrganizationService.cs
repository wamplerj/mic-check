using System.Diagnostics.CodeAnalysis;
using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Organizations;

public class OrganizationService(IMicCheckDbContext db, AuditService auditService)
{
    public async Task<IReadOnlyList<(Organization Org, bool IsPrimary)>> ListForUserAsync(int userId, CancellationToken ct = default)
    {
        var rows = await (
            from org in db.Organizations
            join member in db.OrganizationUsers on org.Id equals member.OrganizationId
            where member.UserId == userId
            select new { org, member.IsPrimary }
        ).ToListAsync(ct);

        return rows.Select(r => (r.org, r.IsPrimary)).ToList();
    }

    public async Task<Organization?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Organization> CreateAsync(string name, int creatorUserId, CancellationToken ct = default)
    {
        var hasExistingOrg = await db.OrganizationUsers.AnyAsync(ou => ou.UserId == creatorUserId, ct);

        var org = new Organization
        {
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };
        org.Members.Add(new OrganizationUser
        {
            UserId = creatorUserId,
            Role = OrganizationRole.Admin,
            IsPrimary = !hasExistingOrg
        });
        db.Organizations.Add(org);
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync("Organization", org.Id.ToString(), "created", org.Id, ct: ct);

        return org;
    }

    [ExcludeFromCodeCoverage(Justification = "ExecuteUpdateAsync requires a real EF Core relational query provider; our Mock<DbSet<T>> LINQ-to-Objects provider can't execute it, and CLAUDE.md disallows the EF InMemory provider as a substitute. The not-a-member early-return branch is covered by OrganizationServiceTests.")]
    public async Task SetPrimaryAsync(int organizationId, int userId, CancellationToken ct = default)
    {
        var member = await db.OrganizationUsers
            .FirstOrDefaultAsync(ou => ou.OrganizationId == organizationId && ou.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"User is not a member of organization {organizationId}.");

        await db.OrganizationUsers
            .Where(ou => ou.UserId == userId && ou.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(ou => ou.IsPrimary, false), ct);

        member.IsPrimary = true;
        await db.SaveChangesAsync(ct);
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

    public async Task<string> GetOrCreateInviteTokenAsync(int organizationId, CancellationToken ct = default)
    {
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct)
            ?? throw new KeyNotFoundException($"Organization {organizationId} not found.");

        if (org.InviteToken is null)
        {
            org.InviteToken = Guid.NewGuid().ToString("N");
            await db.SaveChangesAsync(ct);
        }

        return org.InviteToken;
    }

    public async Task<string> RegenerateInviteTokenAsync(int organizationId, CancellationToken ct = default)
    {
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct)
            ?? throw new KeyNotFoundException($"Organization {organizationId} not found.");

        org.InviteToken = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct);
        return org.InviteToken;
    }

    public async Task<Organization?> FindByInviteTokenAsync(string token, CancellationToken ct = default) =>
        await db.Organizations.FirstOrDefaultAsync(o => o.InviteToken == token, ct);

    public async Task<IReadOnlyList<InviteByEmailResult>> InviteUsersByEmailAsync(int organizationId, IReadOnlyList<InviteByEmailEntry> invites, CancellationToken ct = default)
    {
        var results = new List<InviteByEmailResult>();

        foreach (var invite in invites)
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == invite.Email.Trim().ToLower() && u.IsActive, ct);

            if (user is null)
            {
                results.Add(new InviteByEmailResult(invite.Email, false, "No user found with that email address."));
                continue;
            }

            if (!Enum.TryParse<OrganizationRole>(invite.Role, ignoreCase: true, out var role))
            {
                results.Add(new InviteByEmailResult(invite.Email, false, $"Invalid role '{invite.Role}'."));
                continue;
            }

            await InviteUserAsync(organizationId, user.Id, role, ct);
            results.Add(new InviteByEmailResult(invite.Email, true, null));
        }

        return results;
    }

    public async Task AcceptInviteAsync(string token, int userId, CancellationToken ct = default)
    {
        var org = await FindByInviteTokenAsync(token, ct)
            ?? throw new KeyNotFoundException("Invalid or expired invite link.");

        await InviteUserAsync(org.Id, userId, OrganizationRole.User, ct);
    }
}
