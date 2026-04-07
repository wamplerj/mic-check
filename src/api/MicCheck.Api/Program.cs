using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicCheck.Api.ApiKeys;
using MicCheck.Api.Auth;
using MicCheck.Api.Authentication;
using MicCheck.Api.Authorization;
using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Features;
using MicCheck.Api.Identities;
using MicCheck.Api.Segments;
using MicCheck.Api.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .ReadFrom.Services(services)
              .WriteTo.Console());

    builder.Services.AddOpenApi();
    builder.Services.AddControllers();

    builder.Services.AddAuthentication()
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
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
            };
        });

    builder.Services.AddAuthorization(options =>
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

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddRateLimiter(options =>
        options.AddFixedWindowLimiter("AdminApi", limiter =>
        {
            limiter.PermitLimit = 500;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        }));

    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    builder.Services.AddMemoryCache();

    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<ApiKeyService>();
    builder.Services.AddScoped<DatabaseSeeder>();
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<IAuthorizationHandler, ProjectPermissionRequirementHandler>();
    builder.Services.AddScoped<FeatureEvaluationService>();
    builder.Services.AddScoped<IdentityResolutionService>();
    builder.Services.AddScoped<EnvironmentDocumentService>();
    builder.Services.AddSingleton<SegmentEvaluator>();
    builder.Services.AddSingleton<FlagCache>();

    var connectionString = System.Environment.GetEnvironmentVariable("DATABASE_URL") is { } databaseUrl
        ? DatabaseUrlParser.ToNpgsqlConnectionString(databaseUrl)
        : builder.Configuration.GetConnectionString("DefaultConnection")!;

    builder.Services.AddDbContext<MicCheckDbContext>(options =>
        options.UseNpgsql(connectionString));

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();

        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    app.UseSerilogRequestLogging();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapAuthEndpoints();
    app.MapApiKeyEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
