using WebClientBffGateway.Models;
using WebClientBffGateway.Models.ComblexModels;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpClientDefaults(static http =>
{
    http.AddServiceDiscovery();
});
builder.Services.AddServiceDiscovery();
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


//you forgot to...
//Docker run commands...

//docker run -p 6379:6379 --name patient_monitoring_redis -d redis:latest
//docker run -e "RABBITMQ_DEFAULT_USER=guest" -e "RABBITMQ_DEFAULT_PASS=guest" -p 5672:5672 -p 15672:15672 --name rabbitmq -d rabbitmq:3-management
//docker run -e "POSTGRES_USER=admin" -e "POSTGRES_PASSWORD=admin" -e "POSTGRES_DB=patientdb" -p 3764:5432 --name postgresql -d postgres:15
//docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Password" -p 9933:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2019-latest


