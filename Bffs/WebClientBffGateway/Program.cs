using System.Text.Json;
using WebClientBffGateway.Models;
using WebClientBffGateway.Models.ComblexModels;
using Yarp.ReverseProxy.Forwarder;

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

//builder.Services.AddSingleton<IForwarderHttpClientFactory, CustomForwarderHttpClientFactory>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();
app.MapOpenApi();
app.MapReverseProxy();

app.MapGet("fullData", async (int patientId, HttpClient client, IConfiguration configuration) =>
{
    var monitorUrl = "http://patientmonitoringservice";
    var patientDataUrl = "http://patientdataapi";
    var monitorDataTask = client.GetStringAsync($"{monitorUrl}/vitals/{patientId}");
    var userDataTask = client.GetFromJsonAsync<PatientBasicInfoModel>($"{patientDataUrl}/api/patients/{patientId}");
    await Task.WhenAll(monitorDataTask, userDataTask);
    var monitorDataString = await monitorDataTask;
    var userData = await userDataTask;

    // Deserialize the string to VitalsData object
    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    var vitalsData = JsonSerializer.Deserialize<VitalsData>(monitorDataString, options) ?? new VitalsData();
    
    var result = new FullPatientCurrentStatusData(userData, vitalsData);
    return Results.Ok(result);
});

app.Run();
