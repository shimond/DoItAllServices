using PatientDataAPI.DataContext;

namespace PatientDataAPI.Extensions;

public static class WebApplicationExtensions
{
    public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PatientDataDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        return app;
    }
}