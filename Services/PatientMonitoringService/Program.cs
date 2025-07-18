using Microsoft.Extensions.AI;
using PatientMonitoringService.Extensions;
using PatientMonitoringService.Endpoints;
using Infra.Messaging.Rabbit;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Add patient monitoring services
builder.Services.AddPatientMonitoringServices(builder.Configuration);

// Add RabbitMQ event bus
builder.AddRabbitMQEventBus();

// Add Ollama embedding generator
var ollamaEmbedding = builder.AddOllamaApiClient("ollama-nomic-embed-text");
ollamaEmbedding.AddEmbeddingGenerator();

// Add HTTP clients
builder.Services.AddPatientMonitoringHttpClients();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapOpenApi();

app.MapVitalsEndpoints();

app.Run();