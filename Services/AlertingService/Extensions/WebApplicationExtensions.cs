using AlertingService.Hubs;

namespace AlertingService.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureAlertingPipeline(this WebApplication app)
    {
        // Configure CORS
        app.UseCors();
        
        // Map SignalR Hub
        app.MapHub<VitalsHub>("/vitalsHub");
        
        return app;
    }
}