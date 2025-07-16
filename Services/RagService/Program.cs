using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton(sp =>
{
    var uri = new Uri(builder.Configuration["services:qdrant:qdrant-grpc:0"] ?? "tcp://localhost:21925");
    var client = new QdrantGrpcClient(uri.Host, (int)uri.Port);
    return client;
});

var app = builder.Build();

app.MapOpenApi();

const string collectionName = "patient_vitals";

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
                    { "patientId", new Qdrant.Client.Grpc.Value { StringValue = req.PatientId } },
                    { "patientName", new Qdrant.Client.Grpc.Value { StringValue = req.PatientName ?? "" } }
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
    var payloads = results.Result.OrderByDescending(o => o.Score).Select(r => r.Payload.TryGetValue("text", out var text) ? text.StringValue : null).ToArray();
    return Results.Ok(payloads);
});

// New endpoint to search by patient name
app.MapPost("/rag/query-by-patient", async (RagPatientQueryRequest req, QdrantGrpcClient qdrant) =>
{
    var filter = new Qdrant.Client.Grpc.Filter
    {
        Should =
        {
            new Qdrant.Client.Grpc.Condition
            {
                Field = new Qdrant.Client.Grpc.FieldCondition
                {
                    Key = "patientName",
                    Match = new Qdrant.Client.Grpc.Match
                    {
                        Text = req.PatientName
                    }
                }
            }
        }
    };

    var search = new Qdrant.Client.Grpc.ScrollPoints
    {
        CollectionName = collectionName,
        Filter = filter,
        Limit = (uint)req.TopK,
        WithPayload = new Qdrant.Client.Grpc.WithPayloadSelector { Enable = true }
    };
    
    var results = await qdrant.Points.ScrollAsync(search);
    var payloads = results.Result.Select(r => r.Payload.TryGetValue("text", out var text) ? text.StringValue : null).ToArray();
    return Results.Ok(payloads);
});

app.Run();

public record RagUpsertRequest(string PatientId, string Text, float[] Vector, string? PatientName = null);
public record RagQueryRequest(string Query, int TopK = 3);
public record RagPatientQueryRequest(string PatientName, int TopK = 10);
