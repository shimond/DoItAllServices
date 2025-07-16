using Infra.Messaging.Rabbit;
using Microsoft.EntityFrameworkCore;
using PatientHistoryService.DataAccess;

var builder = WebApplication.CreateBuilder(args); 
 
builder.AddServiceDefaults();
builder .Services.AddDbContext<PatientDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("patientDataHistoryDb")));
builder.Services.AddOpenApi();

builder.AddRabbitMQEventBus();
builder.Services.AddHostedService<PatientHistoryService.PatientHistoryService>();

// Add HTTP client for PatientDataAPI
builder.Services.AddHttpClient("patientdataapi", c =>
{
    c.BaseAddress = new Uri("http://patientdataapi");
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapOpenApi();

app.Use(async (context, next) =>
{
    await next();
});

app.MapGet("/history/{patientId}", async (int patientId, PatientDbContext context, IHttpClientFactory httpClientFactory) =>
{
    var vitalsHistory = await context.VitalsHistory.Where(x=> x.PatientId == patientId).ToListAsync();
    
    if (!vitalsHistory.Any())
        return Results.NotFound();

    // Try to get patient name for enhanced response
    string patientName = $"Patient {patientId}";
    try
    {
        var patientDataClient = httpClientFactory.CreateClient("patientdataapi");
        var patientResponse = await patientDataClient.GetFromJsonAsync<PatientInfo>($"/api/patients/{patientId}");
        if (patientResponse != null)
        {
            patientName = $"{patientResponse.FirstName} {patientResponse.LastName}";
        }
    }
    catch
    {
        // If patient name lookup fails, continue with default name
    }

    var enhancedResponse = new
    {
        PatientId = patientId,
        PatientName = patientName,
        VitalsHistory = vitalsHistory
    };

    return Results.Ok(enhancedResponse);
});

// New endpoint to search history by patient name
app.MapGet("/history/search", async (string patientName, PatientDbContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var patientDataClient = httpClientFactory.CreateClient("patientdataapi");
        var searchResponse = await patientDataClient.GetFromJsonAsync<PatientInfo[]>($"/api/patients/search?fullName={Uri.EscapeDataString(patientName)}");
        
        if (searchResponse?.Any() != true)
            return Results.NotFound($"No patients found with name: {patientName}");

        var results = new List<object>();
        foreach (var patient in searchResponse)
        {
            var vitalsHistory = await context.VitalsHistory.Where(x => x.PatientId == patient.Id).ToListAsync();
            results.Add(new
            {
                PatientId = patient.Id,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                VitalsHistory = vitalsHistory
            });
        }

        return Results.Ok(results);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error searching for patient: {ex.Message}");
    }
});

app.Run();

public class PatientInfo
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; }
    public string Address { get; set; }
}
