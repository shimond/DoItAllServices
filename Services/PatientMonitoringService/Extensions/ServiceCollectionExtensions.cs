using Microsoft.Extensions.AI;
using PatientMonitoringService.Services;
using StackExchange.Redis;
using Infra.Messaging.Rabbit;

namespace PatientMonitoringService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPatientMonitoringServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add OpenAPI
        services.AddOpenApi();
        
        // Add Redis connection
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("cacheDb")!)
        );
        
        // Add patient vitals service
        services.AddScoped<IPatientVitalsService, PatientVitalsService>();
        
        return services;
    }

    public static IServiceCollection AddPatientMonitoringHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient("ragservice", c =>
        {
            c.BaseAddress = new Uri("http://ragservice");
        });

        services.AddHttpClient("patientdataapi", c =>
        {
            c.BaseAddress = new Uri("http://patientdataapi");
        });
        
        return services;
    }
}