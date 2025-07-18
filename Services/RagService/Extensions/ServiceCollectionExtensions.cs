using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace RagService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRagServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add OpenAPI
        services.AddOpenApi();
        
        // Add Qdrant client (using the original approach from Program.cs)
        services.AddSingleton(sp =>
        {
            var uri = new Uri(configuration["services:qdrant:qdrant-grpc:0"] ?? "tcp://localhost:21925");
            var client = new QdrantGrpcClient(uri.Host, (int)uri.Port);
            return client;
        });
        
        return services;
    }
}