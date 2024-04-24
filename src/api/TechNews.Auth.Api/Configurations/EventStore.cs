using EventStore.Client;
using TechNews.Common.Library.Services;

namespace TechNews.Auth.Api.Configurations;

public static class EventStore
{
    public static IServiceCollection ConfigureEventStore(this IServiceCollection services)
    {
        var settings = EventStoreClientSettings.Create(EnvironmentVariables.EventStoreConnectionString);

        var eventStoreClient = new EventStoreClient(settings);
        services.AddSingleton<IEventStoreService>(new EventStoreService(eventStoreClient));

        return services;
    }

}