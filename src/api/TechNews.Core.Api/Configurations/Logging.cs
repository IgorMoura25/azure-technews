using Serilog;
using Serilog.Events;
using Serilog.Sinks.Discord;

namespace TechNews.Core.Api.Configurations;

public static class Logging
{
    public static IServiceCollection AddLoggingConfiguration(this IServiceCollection services, ConfigureHostBuilder host)
    {
        const string template = "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}";

        var webhookId = Convert.ToUInt64(EnvironmentVariables.DiscordWebhookId);
        var webhookToken = EnvironmentVariables.DiscordWebhookToken;

        host.UseSerilog((_, configuration) => configuration
            .WriteTo.Console(outputTemplate: template, restrictedToMinimumLevel: LogEventLevel.Debug)
            //.WriteTo.File(outputTemplate: template, restrictedToMinimumLevel: LogEventLevel.Debug, path: "Logs/log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, buffered: true)
            .WriteTo.Discord(webhookId: webhookId, webhookToken: webhookToken, restrictedToMinimumLevel: LogEventLevel.Warning)
            .WriteTo.Seq(serverUrl: "http://localhost:5341", restrictedToMinimumLevel: LogEventLevel.Verbose)
            .Enrich.WithCorrelationId(headerName: "x-correlation-id", addValueIfHeaderAbsence: true)
            .MinimumLevel.Verbose()
        );

        return services;
    }
    
    public static WebApplication UseLoggingConfiguration(this WebApplication application)
    {
        application.UseSerilogRequestLogging();
        
        return application;
    }
}