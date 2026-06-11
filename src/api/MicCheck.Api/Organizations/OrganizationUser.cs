using MicCheck.Api.Users;

namespace MicCheck.Api.Organizations;

public class OrganizationUser
{
    public int OrganizationId { get; init; }
    public int UserId { get; init; }
    public OrganizationRole Role { get; set; }
    public bool IsPrimary { get; set; }
    public User User { get; init; } = null!;
}

public enum OrganizationRole { User, Admin }
