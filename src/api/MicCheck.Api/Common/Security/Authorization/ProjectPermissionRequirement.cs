using Microsoft.AspNetCore.Authorization;

namespace MicCheck.Api.Common.Security.Authorization;

public class ProjectPermissionRequirement : IAuthorizationRequirement
{
    public ProjectPermission Permission { get; }

    public ProjectPermissionRequirement(ProjectPermission permission) => Permission = permission;
}
