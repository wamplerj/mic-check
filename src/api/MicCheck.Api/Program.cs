using System.Threading.RateLimiting;
using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Common.Security.ApiKeys;
using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Common.Validation;
using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Features;
using MicCheck.Api.Identities;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Segments;
using MicCheck.Api.Users;
using MicCheck.Api.Webhooks;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .ReadFrom.Services(services)
              .WriteTo.Console());

    builder.Services.AddOpenApi();
    builder.Services.AddControllers(options => options.Filters.Add<ModelValidationActionFilter>())
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter()));

    builder.Services.AddCommonServices(builder.Configuration);

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddRateLimiter(options =>
        options.AddFixedWindowLimiter("AdminApi", limiter =>
        {
            limiter.PermitLimit = 500;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        }));

    builder.Services.AddMemoryCache();
    builder.Services.AddMetrics();

    builder.Services.AddAuditServices();
    builder.Services.AddIdentitiesServices();
    builder.Services.AddEnvironmentsServices();
    builder.Services.AddSegmentsServices();
    builder.Services.AddOrganizationsServices();
    builder.Services.AddProjectsServices();
    builder.Services.AddFeaturesServices();
    builder.Services.AddUsersServices();
    builder.Services.AddWebhooksServices();

    builder.Services.AddDataServices(builder.Configuration);

    var app = builder.Build();

    await app.ApplyMigrationsAsync();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

        await app.SeedDevelopmentDataAsync();
    }

    app.UseSerilogRequestLogging();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapDefaultEndpoints();
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
