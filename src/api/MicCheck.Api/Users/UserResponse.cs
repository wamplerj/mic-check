namespace MicCheck.Api.Users;

public record UserResponse(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.CreatedAt, user.LastLoginAt);
}
