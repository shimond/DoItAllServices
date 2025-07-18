using AlertingService.Hubs;

namespace AlertingService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlertingServices(this IServiceCollection services)
    {
        // Add SignalR
        services.AddSignalR();
        
        // Add hosted service for monitoring vitals
        services.AddHostedService<VitalsMonitorWorker>();
        
        return services;
    }

    public static IServiceCollection AddAlertingCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()  // Required for SignalR with WebSockets
                      .WithOrigins("http://localhost:4300"); // Update with your Angular app's URL
            });
        });
        
        return services;
    }
}