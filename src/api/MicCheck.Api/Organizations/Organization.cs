using MicCheck.Api.Common.Security.ApiKeys;
using MicCheck.Api.Projects;

namespace MicCheck.Api.Organizations;

public class Organization
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<Project> Projects { get; init; } = [];
    public ICollection<OrganizationUser> Members { get; init; } = [];
    public ICollection<ApiKey> ApiKeys { get; init; } = [];
    public string? InviteToken { get; set; }
}
