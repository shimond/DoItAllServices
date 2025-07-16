using Microsoft.EntityFrameworkCore;
using PatientDataAPI.DataContext;
using PatientDataAPI.DataEntities;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<PatientDataDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("patientDataDb"));
});

var app = builder.Build();

app.MapDefaultEndpoints();

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

// New endpoint to search patients by name
app.MapGet("/api/patients/search", async (string? firstName, string? lastName, string? fullName, PatientDataDbContext dbContext) =>
{
    var query = dbContext.Patients.AsQueryable();

    if (!string.IsNullOrEmpty(fullName))
    {
        // Handle full name search - split and search for parts
        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nameParts.Length >= 2)
        {
            var firstPart = nameParts[0];
            var lastPart = nameParts[nameParts.Length - 1];
            query = query.Where(p => 
                (p.FirstName.ToLower().Contains(firstPart.ToLower()) && p.LastName.ToLower().Contains(lastPart.ToLower())) ||
                (p.FirstName.ToLower().Contains(lastPart.ToLower()) && p.LastName.ToLower().Contains(firstPart.ToLower())));
        }
        else
        {
            // Single name, search in both first and last name
            query = query.Where(p => p.FirstName.ToLower().Contains(fullName.ToLower()) || 
                                   p.LastName.ToLower().Contains(fullName.ToLower()));
        }
    }
    else
    {
        if (!string.IsNullOrEmpty(firstName))
        {
            query = query.Where(p => p.FirstName.ToLower().Contains(firstName.ToLower()));
        }
        if (!string.IsNullOrEmpty(lastName))
        {
            query = query.Where(p => p.LastName.ToLower().Contains(lastName.ToLower()));
        }
    }

    return await query.ToListAsync();
});

app.MapPost("/api/patients", async (PatientEntity patient, PatientDataDbContext dbContext) =>
{
    dbContext.Patients.Add(patient);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/patients/{patient.Id}", patient);
});

app.Run();

