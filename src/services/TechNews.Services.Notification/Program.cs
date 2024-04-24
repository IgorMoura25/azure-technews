using TechNews.Services.Notification;
using TechNews.Services.Notification.Configurations;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddEnvironmentVariables(builder.Environment)
    .AddLoggingConfiguration()
    .ConfigureMessageBroker()
    .AddHostedService<Worker>();

var host = builder.Build();
    
host.Run();
