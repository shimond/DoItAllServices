using Infra.Messaging.Rabbit;
using Microsoft.EntityFrameworkCore;
using PatientHistoryService.DataAccess;

var builder = WebApplication.CreateBuilder(args); 
 
builder .Services.AddDbContext<PatientDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("patinetDataHistoryDb"))
   );
builder.Services.AddOpenApi();

builder.AddRabbitMQEventBus();
builder.Services.AddHostedService<PatientHistoryService.PatientHistoryService>();

var app = builder.Build();

app.MapOpenApi();

app.Use(async (context, next) =>
{
    await next();
});

app.MapGet("/history/{patientId}", async (int patientId, PatientDbContext context) =>
{
    var res = await context.VitalsHistory.Where(x=> x.PatientId == patientId).ToListAsync();
    return res is not null ? Results.Ok(res) : Results.NotFound();
});

app.Run();
