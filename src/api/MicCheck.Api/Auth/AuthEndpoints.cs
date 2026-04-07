using Microsoft.AspNetCore.Mvc;

namespace MicCheck.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/token", (
            [FromBody] TokenRequest request,
            ITokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest("Username and password are required.");

            var response = tokenService.GenerateToken(request.Username);
            return Results.Ok(response);
        })
        .WithName("GetToken")
        .WithTags("Auth")
        .AllowAnonymous();
    }
}
