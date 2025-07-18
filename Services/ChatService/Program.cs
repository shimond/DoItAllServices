using Microsoft.Extensions.AI;
using ChatService.Extensions;
using ChatService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Add API explorer for development
builder.Services.AddEndpointsApiExplorer();

// Add AI services
builder.AddOpenAIClient("openai")
    .AddChatClient()
    .UseOpenTelemetry();

var ollamaEmbedding = builder.AddOllamaApiClient("ollama-nomic-embed-text");
ollamaEmbedding.AddEmbeddingGenerator();

// Add HTTP clients
builder.Services.AddChatServiceHttpClients();

var app = builder.Build();

// Configure pipeline
app.MapDefaultEndpoints();

// Map endpoints
app.MapChatEndpoints();

app.Run();
