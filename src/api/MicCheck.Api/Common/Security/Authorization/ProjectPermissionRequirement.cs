using Microsoft.AspNetCore.Authorization;

namespace MicCheck.Api.Common.Security.Authorization;

public record ProjectPermissionRequirement(ProjectPermission Permission) : IAuthorizationRequirement;
