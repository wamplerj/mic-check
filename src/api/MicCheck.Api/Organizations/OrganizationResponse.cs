namespace MicCheck.Api.Organizations;

public record OrganizationResponse(
    int Id,
    string Name,
    DateTimeOffset CreatedAt,
    bool IsPrimary
)
{
    public static OrganizationResponse From(Organization org, bool isPrimary = false) => new(
        org.Id, org.Name, org.CreatedAt, isPrimary);
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
