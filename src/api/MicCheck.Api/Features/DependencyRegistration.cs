using MicCheck.Api.Features.Usage;

namespace MicCheck.Api.Features;

public static class DependencyRegistration
{
    public static IServiceCollection AddFeaturesServices(this IServiceCollection services)
    {
        services.AddScoped<FeatureEvaluationService>();
        services.AddSingleton<FlagCache>();
        services.AddScoped<FeatureService>();
        services.AddScoped<FeatureStateService>();
        services.AddScoped<FeatureSegmentService>();
        services.AddScoped<TagService>();

        services.AddFeatureUsageServices();

        return services;
    }
}
