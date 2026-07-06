namespace MicCheck.Api.Webhooks;

public static class DependencyRegistration
{
    public static IServiceCollection AddWebhooksServices(this IServiceCollection services)
    {
        services.AddScoped<WebhookService>();
        services.AddScoped<WebhookDispatcher>();
        services.AddSingleton<WebhookQueue>();
        services.AddHostedService<WebhookBackgroundService>();
        services.AddHostedService<WebhookRetryBackgroundService>();
        services.AddHttpClient("Webhooks", client =>
            client.DefaultRequestHeaders.Add("User-Agent", "MicCheck-Webhook/1.0"));

        return services;
    }
}
