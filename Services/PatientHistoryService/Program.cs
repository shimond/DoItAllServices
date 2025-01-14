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

//using (var scope = app.Services.CreateScope())
//{
//    await Task.Delay(5000);
//    var dbContext = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
//    dbContext.Database.EnsureCreated();
//}

app.MapGet("/history/{patientId}", async (int patientId, PatientDbContext context) =>
{
    var res = await context.VitalsHistory.Where(x=> x.PatientId == patientId).ToListAsync();
    return res is not null ? Results.Ok(res) : Results.NotFound();
});

app.Run();


// add endpoint to get data by patient
// add end point that bring data by the values
// add configuration to yarp that
