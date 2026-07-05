using MicCheck.Api.Common.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Users;

[ApiController]
[Route("api/v1/user")]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class UsersController(UserService userService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct)
    {
        var user = await userService.FindByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(UserResponse.From(user));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await userService.FindByIdAsync(userId.Value, ct);
        return user is null ? NotFound() : Ok(UserResponse.From(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateMe(UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("First name, last name, and email are required.");

        var result = await userService.UpdateProfileAsync(
            userId.Value, request.FirstName.Trim(), request.LastName.Trim(), request.Email.Trim(), ct);

        if (result.EmailConflict) return Conflict("Email is already in use.");
        return result.User is null ? NotFound() : Ok(UserResponse.From(result.User));
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("Current and new passwords are required.");

        var success = await userService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword, ct);
        return success ? NoContent() : BadRequest("Current password is incorrect.");
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
