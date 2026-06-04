using System.Security.Claims;
using System.Text.Encodings.Web;
using MicCheck.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MicCheck.Api.Common.Security.Authentication;

public class EnvironmentKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, MicCheckDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "EnvironmentKey";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Environment-Key", out var keyValues))
            return AuthenticateResult.NoResult();

        var key = keyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
            return AuthenticateResult.Fail("X-Environment-Key header is empty.");

        var environment = await db.Environments
            .FirstOrDefaultAsync(e => e.ApiKey == key);

        if (environment is null)
            return AuthenticateResult.Fail("Invalid environment key.");

        var claims = new[]
        {
            new Claim("EnvironmentId", environment.Id.ToString()),
            new Claim("ProjectId", environment.ProjectId.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
