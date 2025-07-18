namespace ChatService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatServiceHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient("ragservice", c =>
        {
            c.BaseAddress = new Uri("http://ragservice");
        });

        services.AddHttpClient("patienthistoryservice", c =>
        {
            c.BaseAddress = new Uri("http://patienthistoryservice");
        });

        services.AddHttpClient("patientdataapi", c =>
        {
            c.BaseAddress = new Uri("http://patientdataapi");
        });

        return services;
    }
}