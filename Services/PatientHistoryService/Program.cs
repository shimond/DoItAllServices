using PatientHistoryService.Extensions;
using PatientHistoryService.Endpoints;
using Infra.Messaging.Rabbit;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Add patient history services
builder.Services.AddPatientHistoryServices(builder.Configuration);

// Add RabbitMQ event bus
builder.AddRabbitMQEventBus();

// Add HTTP clients

var app = builder.Build();

// Configure pipeline
app.MapDefaultEndpoints();
app.MapOpenApi();


// Map endpoints
app.MapHistoryEndpoints();

app.Run();
