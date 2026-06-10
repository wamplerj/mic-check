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
    string FirstName,
    string LastName,
    string Email,
    string Role,
    DateTimeOffset? LastLoginAt
)
{
    public static OrganizationMemberResponse From(OrganizationUser member) => new(
        member.UserId,
        member.User.FirstName,
        member.User.LastName,
        member.User.Email,
        member.Role.ToString(),
        member.User.LastLoginAt);
}
