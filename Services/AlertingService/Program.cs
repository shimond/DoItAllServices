using AlertingService.Extensions;
using Infra.Messaging.Rabbit;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Add RabbitMQ event bus
builder.AddRabbitMQEventBus();

// Add alerting services
builder.Services.AddAlertingServices();

// Add CORS configuration
builder.Services.AddAlertingCors();

var app = builder.Build();

// Configure pipeline
app.MapDefaultEndpoints();

// Configure alerting pipeline
app.ConfigureAlertingPipeline();

app.Run();

