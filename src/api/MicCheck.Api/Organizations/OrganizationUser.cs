namespace MicCheck.Api.Organizations;

public class OrganizationUser
{
    public int OrganizationId { get; init; }
    public int UserId { get; init; }
    public OrganizationRole Role { get; set; }
}

public enum OrganizationRole { User, Admin }
