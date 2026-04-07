using Microsoft.AspNetCore.Authorization;

namespace MicCheck.Api.Authorization;

public class ProjectPermissionRequirement : IAuthorizationRequirement
{
    public ProjectPermission Permission { get; }

    public ProjectPermissionRequirement(ProjectPermission permission) => Permission = permission;
}
