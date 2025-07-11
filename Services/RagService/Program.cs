using Qdrant.Client;
using Qdrant.Client.Grpc;
using Qdrant.Client.Grpc.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton(sp =>
{
    // Qdrant running as container, use service name and port
    var client = new QdrantGrpcClient("qdrant", 6333);
    return client;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

const string collectionName = "patient_vitals";

// Upsert endpoint
app.MapPost("/rag/upsert", async (RagUpsertRequest req, QdrantGrpcClient qdrant) =>
{
    // Ensure collection exists
    await qdrant.CreateCollectionAsync(collectionName, new VectorParams { Size = req.Vector.Length, Distance = Distance.Cosine });

    var point = new PointStruct
    {
        Id = req.PatientId,
        Payload = new() { ["text"] = req.Text },
        Vector = req.Vector
    };
    await qdrant.UpsertPointsAsync(collectionName, new[] { point });
    return Results.Ok();
});

// Query endpoint
app.MapPost("/rag/query", async (RagQueryRequest req, QdrantGrpcClient qdrant) =>
{
    // For demo: expects req.Query to be a vector serialized as JSON array
    // In real use, embed the query text to a vector first
    float[] queryVector;
    try
    {
        queryVector = JsonSerializer.Deserialize<float[]>(req.Query) ?? Array.Empty<float>();
    }
    catch
    {
        return Results.BadRequest("Query must be a float array serialized as JSON.");
    }
    var results = await qdrant.SearchPointsAsync(collectionName, queryVector, req.TopK);
    var payloads = results.Select(r => r.Payload["text"]?.ToString()).ToArray();
    return Results.Ok(payloads);
});

app.Run();

public record RagUpsertRequest(string PatientId, string Text, float[] Vector);
public record RagQueryRequest(string Query, int TopK = 3);
