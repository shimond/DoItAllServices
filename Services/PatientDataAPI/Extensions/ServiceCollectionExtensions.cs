using Microsoft.EntityFrameworkCore;
using PatientDataAPI.DataContext;

namespace PatientDataAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPatientDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PatientDataDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("patientDataDb"));
        });


        return services;
    }
}