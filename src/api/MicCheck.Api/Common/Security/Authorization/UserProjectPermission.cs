namespace MicCheck.Api.Common.Security.Authorization;

public class UserProjectPermission
{
    public int UserId { get; init; }
    public int ProjectId { get; init; }
    public List<ProjectPermission> Permissions { get; set; } = [];
    public bool IsAdmin { get; set; }
}
