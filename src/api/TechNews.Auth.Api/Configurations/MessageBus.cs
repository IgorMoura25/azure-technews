using TechNews.Common.Library.MessageBus;
using TechNews.Common.Library.MessageBus.Brokers.RabbitMQ;

namespace TechNews.Auth.Api.Configurations;

public static class MessageBus
{
    public static IServiceCollection ConfigureMessageBroker(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBus, RabbitMQMessageBus>(_ =>
            new RabbitMQMessageBus(
                new RabbitMQMessageBusParameters(
                    HostName: EnvironmentVariables.BrokerHostName,
                    VirtualHost: EnvironmentVariables.BrokerVirtualHost,
                    Password: EnvironmentVariables.BrokerPassword,
                    UserName: EnvironmentVariables.BrokerUserName
                )
            )
        );
        return services;
    }
}