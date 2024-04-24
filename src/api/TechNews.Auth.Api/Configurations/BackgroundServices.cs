using TechNews.Auth.Api.Services;

namespace TechNews.Auth.Api.Configurations;

public static class BackgroundServices
{
    public static IServiceCollection ConfigureBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<KeyRotatorBackgroundService>();

        return services;
    }
}
