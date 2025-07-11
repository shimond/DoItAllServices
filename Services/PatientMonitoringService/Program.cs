using Infra.Messaging.Rabbit;
using PatientMonitoringService.Models;
using PatientMonitoringService.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("cacheDb"))
);

builder.AddRabbitMQEventBus();
builder.Services.AddScoped<IPatientVitalsService, PatientVitalsService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapOpenApi();


app.Use(async(context, next) =>
{
    await next();
});

app.MapGet("/vitals/makeError/1", async (IPatientVitalsService vitalsService) =>
{
    await Task.Delay(5000);
    return Results.InternalServerError(Results.Problem("An error occurred while processing your request."));
});

app.MapGet("/vitals/{patientId}", async (int patientId, IPatientVitalsService vitalsService) =>
{
    var res = await vitalsService.GetPatientVitalsAsync(patientId);
    return res is not null ? Results.Ok(res) : Results.NotFound();
});

app.MapPost("/vitals", async (VitalsRequest request, IPatientVitalsService vitalsService) =>
{
    await vitalsService.StorePatientVitalsAsync(request.PatientId, request.VitalsData);
    return Results.Ok();
});

app.Run();

public class VitalsRequest
{
    public int PatientId { get; set; }
    public VitalsData VitalsData { get; set; }
}