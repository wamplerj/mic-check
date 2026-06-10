using Microsoft.AspNetCore.Mvc;

namespace MicCheck.Api.Common.Security.Authorization;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").AllowAnonymous().WithTags("Auth");

        group.MapPost("/login", async ([FromBody] LoginRequest request, AuthService authService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest("Email and password are required.");

            var response = await authService.LoginAsync(request.Email, request.Password, ct);
            return response is null
                ? Results.Unauthorized()
                : Results.Ok(response);

        }).WithName("Login");

        group.MapPost("/register", async ([FromBody] RegisterRequest request, AuthService authService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest("Email and password are required.");

            var response = await authService.RegisterAsync(
                request.Email, request.Password,
                request.FirstName, request.LastName,
                request.OrganizationName, ct);

            return response is null
                ? Results.Conflict("An account with this email already exists.")
                : Results.Ok(response);

        }).WithName("Register");

        group.MapPost("/refresh", async ([FromBody] RefreshRequest request, AuthService authService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return Results.BadRequest("Refresh token is required.");

            var response = await authService.RefreshAsync(request.Token, ct);
            return response is null
                ? Results.Unauthorized()
                : Results.Ok(response);

        }).WithName("RefreshToken");

        group.MapPost("/logout", async ([FromBody] LogoutRequest request, AuthService authService, CancellationToken ct) =>
        {
            await authService.LogoutAsync(request.Token, ct);
            return Results.NoContent();

        }).WithName("Logout");
    }
}
