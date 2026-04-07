using MicCheck.Api.Data;
using MicCheck.Api.Features;
using Microsoft.EntityFrameworkCore;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Environments;

public class EnvironmentDocumentService(MicCheckDbContext db)
{
    public async Task<EnvironmentDocumentResponse?> GetAsync(int environmentId, CancellationToken ct = default)
    {
        var environment = await db.Environments
            .FindAsync([environmentId], ct);

        if (environment is null)
            return null;

        var featureStateResults = await (
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

        var project = await db.Projects
            .Include(p => p.Segments)
                .ThenInclude(s => s.Rules)
                    .ThenInclude(r => r.Conditions)
            .Include(p => p.Segments)
                .ThenInclude(s => s.Rules)
                    .ThenInclude(r => r.ChildRules)
                        .ThenInclude(cr => cr.Conditions)
            .FirstOrDefaultAsync(p => p.Id == environment.ProjectId, ct);

        if (project is null)
            return null;

        return new EnvironmentDocumentResponse(
            environment.Id,
            environment.ApiKey,
            featureStateResults.Select(FlagResponse.From).ToList(),
            new EnvironmentProjectResponse(
                project.Id,
                project.Name,
                project.Segments.Select(SegmentDocumentResponse.From).ToList()));
    }
}
