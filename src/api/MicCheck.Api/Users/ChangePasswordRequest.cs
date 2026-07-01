namespace MicCheck.Api.Users;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
