using System.Text;
using MicCheck.Api.Common.Security.ApiKeys;
using MicCheck.Api.Common.Security.Authentication;
using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Common.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MicCheck.Api.Common;

public static class DependencyRegistration
{
    public static IServiceCollection AddCommonServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, EnvironmentKeyAuthenticationHandler>(
                EnvironmentKeyAuthenticationHandler.SchemeName, _ => { })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.SchemeName, _ => { })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!))
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.FlagsApiAccess, policy =>
                policy.AddAuthenticationSchemes(EnvironmentKeyAuthenticationHandler.SchemeName)
                      .RequireClaim("EnvironmentId"));

            options.AddPolicy(AuthorizationPolicies.AdminApiAccess, policy =>
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName, JwtBearerDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser());

            options.AddPolicy(AuthorizationPolicies.OrganizationAdmin, policy =>
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName, JwtBearerDefaults.AuthenticationScheme)
                      .RequireClaim("OrganizationRole", "Admin"));
        });

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<ApiKeyService>();
        services.AddScoped<IAuthorizationHandler, ProjectPermissionRequirementHandler>();

        services.AddModelValidatorsFromAssemblyContaining<Program>();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context => ValidationProblemResponseFactory.Create(context.ModelState);
        });

        return services;
    }
}
