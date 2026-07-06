namespace MicCheck.Api.Segments;

public static class DependencyRegistration
{
    public static IServiceCollection AddSegmentsServices(this IServiceCollection services)
    {
        services.AddSingleton<SegmentEvaluator>();
        services.AddScoped<SegmentService>();

        return services;
    }
}
