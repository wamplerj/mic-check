using System.IdentityModel.Tokens.Jwt;
using MicCheck.Api.Data;
using MicCheck.Api.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Common.Security.Authorization;

public class ProjectPermissionRequirementHandler(
    IMicCheckDbContext db,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<ProjectPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectPermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            context.Fail();
            return;
        }

        if (context.User.HasClaim("OrganizationRole", OrganizationRole.Admin.ToString()))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null ||
            !httpContext.Request.RouteValues.TryGetValue("projectId", out var projectIdValue) ||
            !int.TryParse(projectIdValue?.ToString(), out var projectId))
        {
            context.Fail();
            return;
        }

        var permission = await db.UserProjectPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ProjectId == projectId);

        if (permission is null)
        {
            context.Fail();
            return;
        }

        if (permission.IsAdmin || permission.Permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
