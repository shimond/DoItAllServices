using Microsoft.EntityFrameworkCore;
using PatientHistoryService.DataAccess;
using HistoryService = PatientHistoryService.PatientHistoryService;

namespace PatientHistoryService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPatientHistoryServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework DbContext
        services.AddDbContext<PatientDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("patientDataHistoryDb")));
        
        // Add OpenAPI
        services.AddOpenApi();
        
        // Add hosted service for patient history processing
        services.AddHostedService<HistoryService>();
        
        return services;
    }
}