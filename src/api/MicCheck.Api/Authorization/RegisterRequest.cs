namespace MicCheck.Api.Authorization;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string OrganizationName);
