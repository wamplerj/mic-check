using System.Security.Claims;
using System.Text.Encodings.Web;
using MicCheck.Api.ApiKeys;
using MicCheck.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MicCheck.Api.Authentication;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    MicCheckDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    private const string ApiKeyPrefix = "Api-Key ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var headerValue = authHeader.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(headerValue) ||
            !headerValue.StartsWith(ApiKeyPrefix, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var rawKey = headerValue[ApiKeyPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(rawKey))
            return AuthenticateResult.Fail("API key value is empty.");

        var hashedKey = ApiKeyHasher.Hash(rawKey);

        var apiKey = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.Key == hashedKey);

        if (apiKey is null)
            return AuthenticateResult.Fail("Invalid API key.");

        if (!apiKey.IsActive)
            return AuthenticateResult.Fail("API key is inactive.");

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTimeOffset.UtcNow)
            return AuthenticateResult.Fail("API key has expired.");

        var claims = new[]
        {
            new Claim("OrganizationId", apiKey.OrganizationId.ToString()),
            new Claim("OrganizationRole", "Admin")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
