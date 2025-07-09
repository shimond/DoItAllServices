using AlertingService;
using AlertingService.Hubs;
using Infra.Messaging;
using Infra.Messaging.Rabbit;

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine("Wating for EventBus");

await Task.Delay(30000);
builder.AddRabbitMQEventBus();
builder.Services.AddSignalR();

builder.Services.AddHostedService<VitalsMonitorWorker>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()  // Required for SignalR with WebSockets
              .WithOrigins("http://localhost:4300"); // Update with your Angular app's URL
    });
});
var app = builder.Build();

app.UseCors();
app.MapHub<VitalsHub>("/vitalsHub");


app.Run();

