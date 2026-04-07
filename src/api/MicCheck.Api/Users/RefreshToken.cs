namespace MicCheck.Api.Users;

public class RefreshToken
{
    public int Id { get; init; }
    public required string Token { get; init; }
    public int UserId { get; init; }
    public User User { get; init; } = null!;
    public DateTimeOffset ExpiresAt { get; init; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
