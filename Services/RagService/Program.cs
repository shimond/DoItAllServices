using RagService.Extensions;
using RagService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add RAG services
builder.Services.AddRagServices(builder.Configuration);

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map endpoints
app.MapRagEndpoints();

app.Run();
