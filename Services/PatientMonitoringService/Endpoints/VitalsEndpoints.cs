using Microsoft.Extensions.AI;
using PatientMonitoringService.Models;
using PatientMonitoringService.Services;
using System.Net.Http.Json;

namespace PatientMonitoringService.Endpoints;

public static class VitalsEndpoints
{
    public static void MapVitalsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/vitals").WithTags("Patient Vitals");

        group.MapGet("/makeError/1", CreateTestError);
        group.MapGet("/{patientId:int}", GetPatientVitals);
        group.MapPost("/", StorePatientVitals);
    }

    private static async Task<IResult> CreateTestError(IPatientVitalsService vitalsService)
    {
        await Task.Delay(5000);
        return Results.Problem("An error occurred while processing your request.");
    }

    private static async Task<IResult> GetPatientVitals(int patientId, IPatientVitalsService vitalsService)
    {
        var res = await vitalsService.GetPatientVitalsAsync(patientId);
        return res is not null ? Results.Ok(res) : Results.NotFound();
    }

    private static async Task<IResult> StorePatientVitals(
        VitalsRequest request, 
        IPatientVitalsService vitalsService, 
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,  
        IHttpClientFactory httpClientFactory)
    {
        // Store vitals data
        await vitalsService.StorePatientVitalsAsync(request.PatientId, request.VitalsData);

        // Get patient information for enhanced text
        string patientName = await GetPatientNameAsync(request.PatientId, httpClientFactory);

        // Prepare enhanced text for embedding with patient name
        var text = CreateVitalsEmbeddingText(request, patientName);

        // Get embedding from Ollama
        var embedResp = await embeddingGenerator.GenerateVectorAsync(text);
        if (embedResp.IsEmpty) 
            return Results.Problem("Embedding failed");
        
        // Upsert to RAG with patient name
        await UpsertToRagServiceAsync(request.PatientId, text, embedResp, patientName, httpClientFactory);

        return Results.Ok();
    }

    private static async Task<string> GetPatientNameAsync(int patientId, IHttpClientFactory httpClientFactory)
    {
        var patientDataClient = httpClientFactory.CreateClient("patientdataapi");
        try
        {
            var patientResponse = await patientDataClient.GetFromJsonAsync<PatientInfo>($"/api/patients/{patientId}");
            if (patientResponse != null)
            {
                return $"{patientResponse.FirstName} {patientResponse.LastName}";
            }
        }
        catch
        {
            // Fall back to default name if lookup fails
        }
        return $"Patient {patientId}";
    }

    private static string CreateVitalsEmbeddingText(VitalsRequest request, string patientName)
    {
        return $"Patient {request.PatientId} ({patientName}), " +
               $"Temp: {request.VitalsData.Temperature}C, " +
               $"BP: {request.VitalsData.BloodPressure}, " +
               $"HR: {request.VitalsData.HeartRate}, " +
               $"RR: {request.VitalsData.RespiratoryRate}, " +
               $"SpO2: {request.VitalsData.OxygenSaturation}, " +
               $"Date: {DateTime.UtcNow:O}";
    }

    private static async Task UpsertToRagServiceAsync(
        int patientId, 
        string text, 
        ReadOnlyMemory<float> embedding, 
        string patientName, 
        IHttpClientFactory httpClientFactory)
    {
        var ragClient = httpClientFactory.CreateClient("ragservice");
        var ragReq = new
        {
            PatientId = patientId.ToString(),
            Text = text,
            Vector = embedding,
            PatientName = patientName
        };
        await ragClient.PostAsJsonAsync("/rag/upsert", ragReq);
    }
}