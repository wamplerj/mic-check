using MicCheck.Api.Common.Security.Authorization;

namespace MicCheck.Api.Projects;

public record ProjectResponse(
    int Id,
    string Name,
    int OrganizationId,
    bool HideDisabledFlags,
    DateTimeOffset CreatedAt
)
{
    public static ProjectResponse From(Project project) => new(
        project.Id, project.Name, project.OrganizationId, project.HideDisabledFlags, project.CreatedAt);
}

public record UserPermissionResponse(
    int UserId,
    int ProjectId,
    bool IsAdmin,
    IReadOnlyList<string> Permissions
)
{
    public static UserPermissionResponse From(UserProjectPermission p) => new(
        p.UserId, p.ProjectId, p.IsAdmin,
        p.Permissions.Select(x => x.ToString()).ToList());
}
