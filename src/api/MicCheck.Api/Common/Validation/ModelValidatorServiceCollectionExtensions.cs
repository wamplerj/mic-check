using Microsoft.Extensions.DependencyInjection;

namespace MicCheck.Api.Common.Validation;

public static class ModelValidatorServiceCollectionExtensions
{
    public static IServiceCollection AddModelValidatorsFromAssemblyContaining<TMarker>(this IServiceCollection services)
    {
        var registrations = typeof(TMarker).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IModelValidator<>))
                .Select(i => (Interface: i, Implementation: type)));

        foreach (var (@interface, implementation) in registrations)
            services.AddScoped(@interface, implementation);

        return services;
    }
}
