using Microsoft.EntityFrameworkCore;
using PatientDataAPI.DataContext;
using PatientDataAPI.DataEntities;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<PatientDataDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("patientDataDb"));
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PatientDataDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapGet("/api/patients", async (PatientDataDbContext dbContext) =>
{
    return await dbContext.Patients.ToListAsync();
});

app.MapGet("/api/patients/{id}", async (int id, PatientDataDbContext dbContext) =>
{
    var patient = await dbContext.Patients.FindAsync(id);
    return patient is not null ? Results.Ok(patient) : Results.NotFound();
});

app.MapPost("/api/patients", async (PatientEntity patient, PatientDataDbContext dbContext) =>
{
    dbContext.Patients.Add(patient);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/patients/{patient.Id}", patient);
});

app.Run();

