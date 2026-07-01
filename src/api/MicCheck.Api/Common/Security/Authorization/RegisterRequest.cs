namespace MicCheck.Api.Common.Security.Authorization;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string OrganizationName);
