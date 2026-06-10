using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Features;

public class TagService(MicCheckDbContext db)
{
    public async Task<IReadOnlyList<Tag>> ListByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await db.Tags.Where(t => t.ProjectId == projectId).ToListAsync(ct);
    }

    public async Task<Tag> CreateAsync(int projectId, string label, string color, CancellationToken ct = default)
    {
        var tag = new Tag { Label = label, Color = color, ProjectId = projectId };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
        return tag;
    }

    public async Task<Tag?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null) return;
        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
    }
}
