using MicCheck.Api.Organizations;

namespace MicCheck.Api.Users;

public class User
{
    public int Id { get; init; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public bool IsActive { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public ICollection<OrganizationUser> Organizations { get; init; } = [];
}
