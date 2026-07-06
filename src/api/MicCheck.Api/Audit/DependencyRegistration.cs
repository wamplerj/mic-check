namespace MicCheck.Api.Audit;

public static class DependencyRegistration
{
    public static IServiceCollection AddAuditServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<AuditLogQueryService>();

        return services;
    }
}
