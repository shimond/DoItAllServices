using PatientDataAPI.Endpoints;
using PatientDataAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Add patient data services
builder.Services.AddPatientDataServices(builder.Configuration);

var app = builder.Build();

// Configure pipeline
app.MapDefaultEndpoints();

// Initialize database
await app.InitializeDatabaseAsync();

// Map endpoints
app.MapPatientEndpoints();

app.Run();

