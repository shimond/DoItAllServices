using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton(sp =>
{
    var client = new QdrantGrpcClient("localhost", 6334);
    return client;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

const string collectionName = "patient_vitals";

// Upsert endpoint
app.MapPost("/rag/upsert", async (RagUpsertRequest req, QdrantGrpcClient qdrant) =>
{
    // Ensure collection exists (ignore error if already exists)
    try
    {
        await qdrant.Collections.CreateAsync(new Qdrant.Client.Grpc.CreateCollection
        {
            CollectionName = collectionName,
            VectorsConfig = new Qdrant.Client.Grpc.VectorsConfig
            {
                Params = new Qdrant.Client.Grpc.VectorParams
                {
                    Size = (uint)req.Vector.Length,
                    Distance = Qdrant.Client.Grpc.Distance.Cosine
                }
            }
        });
    }
    catch { /* ignore if already exists */ }

    var upsert = new Qdrant.Client.Grpc.UpsertPoints
    {
        CollectionName = collectionName,
        Points =
        {
            new Qdrant.Client.Grpc.PointStruct
            {
                Id = new Qdrant.Client.Grpc.PointId { Uuid = Guid.NewGuid().ToString() },
                Vectors = new Qdrant.Client.Grpc.Vectors { Vector = new Vector(req.Vector) },
                Payload =
                {
                    { "text", new Qdrant.Client.Grpc.Value { StringValue = req.Text } },
                    { "patientId", new Qdrant.Client.Grpc.Value { StringValue = req.PatientId } }
                }
            }
        }
    };
    await qdrant.Points.UpsertAsync(upsert);
    return Results.Ok();
});

// Query endpoint
app.MapPost("/rag/query", async (RagQueryRequest req, QdrantGrpcClient qdrant) =>
{
    float[] queryVector;
    try
    {
        queryVector = JsonSerializer.Deserialize<float[]>(req.Query) ?? Array.Empty<float>();
    }
    catch
    {
        return Results.BadRequest("Query must be a float array serialized as JSON.");
    }
    var search = new Qdrant.Client.Grpc.SearchPoints
    {
        CollectionName = collectionName,
        Vector = { queryVector },
        Limit = (ulong)req.TopK,
        WithPayload = new Qdrant.Client.Grpc.WithPayloadSelector { Enable = true }
    };
    var results = await qdrant.Points.SearchAsync(search);
    var payloads = results.Result.OrderByDescending(o=> o.Score).Select(r => r.Payload.TryGetValue("text", out var text) ? text.StringValue : null).ToArray();
    return Results.Ok(payloads);
});

app.Run();

public record RagUpsertRequest(string PatientId, string Text, float[] Vector);
public record RagQueryRequest(string Query, int TopK = 3);
