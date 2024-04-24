using Serilog;
using Serilog.Events;
using Serilog.Sinks.Discord;

namespace TechNews.Services.Notification.Configurations;

public static class Logging
{
    public static IServiceCollection AddLoggingConfiguration(this IServiceCollection services)
    {
        const string template = "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}";

        var webhookId = Convert.ToUInt64(EnvironmentVariables.DiscordWebhookId);
        var webhookToken = EnvironmentVariables.DiscordWebhookToken;
        
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: template, restrictedToMinimumLevel: LogEventLevel.Debug)
            .WriteTo.Discord(webhookId: webhookId, webhookToken: webhookToken, restrictedToMinimumLevel: LogEventLevel.Warning)
            .WriteTo.Seq(serverUrl: "http://localhost:5341", restrictedToMinimumLevel: LogEventLevel.Verbose)
            .Enrich.WithCorrelationId(headerName: "x-correlation-id", addValueIfHeaderAbsence: true)
            .MinimumLevel.Verbose()
            .CreateLogger();
        
        return services;
    }
}