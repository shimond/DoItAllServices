using Infra.Messaging.Rabbit;
using Microsoft.Extensions.AI;
using PatientMonitoringService.Models;
using PatientMonitoringService.Services;
using StackExchange.Redis;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("cacheDb"))
);

builder.AddRabbitMQEventBus();
builder.Services.AddScoped<IPatientVitalsService, PatientVitalsService>();

var ollmaEmbedding = builder.AddOllamaApiClient("ollama-nomic-embed-text");
ollmaEmbedding.AddEmbeddingGenerator();

builder.Services.AddHttpClient("ragservice", c =>
{
    c.BaseAddress = new Uri("http://ragservice");
});

// Add HTTP client for PatientDataAPI
builder.Services.AddHttpClient("patientdataapi", c =>
{
    c.BaseAddress = new Uri("http://patientdataapi");
});

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

app.MapPost("/vitals", async (VitalsRequest request, IPatientVitalsService vitalsService, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,  IHttpClientFactory httpClientFactory) =>
{
    await vitalsService.StorePatientVitalsAsync(request.PatientId, request.VitalsData);

    // Fetch patient information for enhanced text
    var patientDataClient = httpClientFactory.CreateClient("patientdataapi");
    string patientName = "";
    try
    {
        var patientResponse = await patientDataClient.GetFromJsonAsync<PatientInfo>($"/api/patients/{request.PatientId}");
        if (patientResponse != null)
        {
            patientName = $"{patientResponse.FirstName} {patientResponse.LastName}";
        }
    }
    catch
    {
        patientName = $"Patient {request.PatientId}";
    }

    // 1. Prepare enhanced text for embedding with patient name
    var text = $"Patient {request.PatientId} ({patientName}), Temp: {request.VitalsData.Temperature}C, BP: {request.VitalsData.BloodPressure}, HR: {request.VitalsData.HeartRate}, RR: {request.VitalsData.RespiratoryRate}, SpO2: {request.VitalsData.OxygenSaturation}, Date: {DateTime.UtcNow:O}";

    // 2. Get embedding from Ollama
    var embedResp = await embeddingGenerator.GenerateVectorAsync(text);
    if (embedResp.IsEmpty) return Results.Problem("Embedding failed");
    
    // 3. Upsert to RAG with patient name
    var ragClient = httpClientFactory.CreateClient("ragservice");
    var ragReq = new
    {
        PatientId = request.PatientId.ToString(),
        Text = text,
        Vector = embedResp,
        PatientName = patientName
    };
    await ragClient.PostAsJsonAsync("/rag/upsert", ragReq);

    return Results.Ok();
});

app.Run();

public class VitalsRequest
{
    public int PatientId { get; set; }
    public VitalsData VitalsData { get; set; }
}

public class PatientInfo
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; }
    public string Address { get; set; }
}

public class OllamaEmbeddingResponse
{
    public float[] embedding { get; set; }
}