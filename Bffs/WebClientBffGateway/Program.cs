using WebClientBffGateway.Models;
using WebClientBffGateway.Models.ComblexModels;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddCors(x => 
                        x.AddDefaultPolicy(o => o.AllowAnyHeader()
                                                .WithOrigins("http://localhost:4300")
                                                .AllowCredentials()
                                                .AllowAnyMethod()));

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("fullData", async (int patientId, HttpClient client, IConfiguration configuration) =>
{
    var monitorUrl = "http://patientmonitoringservice";
    var patientDataUrl = "http://patientdataapi";
    var monitorDataTask = client.GetFromJsonAsync<string>($"{monitorUrl}/vitals/{patientId}");
    var userDataTask = client.GetFromJsonAsync<PatientBasicInfoModel>($"{patientDataUrl}/api/patients/{patientId}");
    await Task.WhenAll(monitorDataTask, userDataTask);
    var monitorData = await monitorDataTask;
    var userData = await userDataTask;
    var result = new FullPatientCurrentStatusData(userData, monitorData);
    return Results.Ok(result);
});

app.UseCors();
app.MapOpenApi();
app.MapReverseProxy();

app.Run();



// client httpClient pool