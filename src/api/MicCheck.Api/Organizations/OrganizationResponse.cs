namespace MicCheck.Api.Organizations;

public record OrganizationResponse(
    int Id,
    string Name,
    DateTimeOffset CreatedAt
)
{
    public static OrganizationResponse From(Organization org) => new(
        org.Id, org.Name, org.CreatedAt);
}

public record OrganizationMemberResponse(
    int UserId,
    string Role
)
{
    public static OrganizationMemberResponse From(OrganizationUser member) => new(
        member.UserId, member.Role.ToString());
}
