using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Text.Json;
using RagService.Models;

namespace RagService.Endpoints;

public static class RagEndpoints
{
    private const string CollectionName = "patient_vitals";

    public static void MapRagEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/rag").WithTags("RAG Operations");

        group.MapPost("/upsert", UpsertToCollection);
        group.MapPost("/query", QueryCollection);
        group.MapPost("/query-by-patient", QueryCollectionByPatient);
    }

    private static async Task<IResult> UpsertToCollection(RagUpsertRequest req, QdrantGrpcClient qdrant)
    {
        // Ensure collection exists (ignore error if already exists)
        await EnsureCollectionExistsAsync(qdrant, req.Vector.Length);

        var upsert = new UpsertPoints
        {
            CollectionName = CollectionName,
            Points =
            {
                new PointStruct
                {
                    Id = new PointId { Uuid = Guid.NewGuid().ToString() },
                    Vectors = new Vectors { Vector = new Vector(req.Vector) },
                    Payload =
                    {
                        { "text", new Value { StringValue = req.Text } },
                        { "patientId", new Value { StringValue = req.PatientId } },
                        { "patientName", new Value { StringValue = req.PatientName ?? "" } }
                    }
                }
            }
        };
        
        await qdrant.Points.UpsertAsync(upsert);
        return Results.Ok();
    }

    private static async Task<IResult> QueryCollection(RagQueryRequest req, QdrantGrpcClient qdrant)
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

        var search = new SearchPoints
        {
            CollectionName = CollectionName,
            Vector = { queryVector },
            Limit = (ulong)req.TopK,
            WithPayload = new WithPayloadSelector { Enable = true }
        };
        
        var results = await qdrant.Points.SearchAsync(search);
        var payloads = ExtractTextPayloads(results.Result);
        return Results.Ok(payloads);
    }

    private static async Task<IResult> QueryCollectionByPatient(RagPatientQueryRequest req, QdrantGrpcClient qdrant)
    {
        var filter = new Filter
        {
            Should =
            {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "patientName",
                        Match = new Match
                        {
                            Text = req.PatientName
                        }
                    }
                }
            }
        };

        var search = new ScrollPoints
        {
            CollectionName = CollectionName,
            Filter = filter,
            Limit = (uint)req.TopK,
            WithPayload = new WithPayloadSelector { Enable = true }
        };
        
        var results = await qdrant.Points.ScrollAsync(search);
        var payloads = ExtractTextPayloadsFromScroll(results.Result);
        return Results.Ok(payloads);
    }

    private static async Task EnsureCollectionExistsAsync(QdrantGrpcClient qdrant, int vectorSize)
    {
        try
        {
            await qdrant.Collections.CreateAsync(new CreateCollection
            {
                CollectionName = CollectionName,
                VectorsConfig = new VectorsConfig
                {
                    Params = new VectorParams
                    {
                        Size = (uint)vectorSize,
                        Distance = Distance.Cosine
                    }
                }
            });
        }
        catch 
        { 
            // Ignore if collection already exists
        }
    }

    private static string?[] ExtractTextPayloads(IEnumerable<ScoredPoint> results)
    {
        return results
            .OrderByDescending(o => o.Score)
            .Select(r => r.Payload.TryGetValue("text", out var text) ? text.StringValue : null)
            .ToArray();
    }

    private static string?[] ExtractTextPayloadsFromScroll(IEnumerable<RetrievedPoint> results)
    {
        return results
            .Select(r => r.Payload.TryGetValue("text", out var text) ? text.StringValue : null)
            .ToArray();
    }
}