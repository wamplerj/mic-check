namespace MicCheck.Api.Features.Usage;

public static class DependencyRegistration
{
    public static IServiceCollection AddFeatureUsageServices(this IServiceCollection services)
    {
        services.AddSingleton<FeatureUsageMetrics>();
        services.AddScoped<FeatureUsageQueryService>();
        services.AddHostedService<FeatureUsageFlushBackgroundService>();

        return services;
    }
}
