namespace MicCheck.Api.Organizations;

public record InviteUsersByEmailRequest(IReadOnlyList<InviteByEmailEntry> Invites);

public record InviteByEmailEntry(string Email, string Role);

public record InviteByEmailResult(string Email, bool Success, string? Error);
