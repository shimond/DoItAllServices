using Microsoft.EntityFrameworkCore;
using PatientDataAPI.DataContext;
using PatientDataAPI.DataEntities;

namespace PatientDataAPI.Endpoints;

public static class PatientEndpoints
{
    public static void MapPatientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/patients").WithTags("Patients");

        group.MapGet("/", GetAllPatients);
        group.MapGet("/{id:int}", GetPatientById);
        group.MapGet("/search", SearchPatients);
        group.MapPost("/", CreatePatient);
    }

    private static async Task<IResult> GetAllPatients(PatientDataDbContext dbContext)
    {
        var patients = await dbContext.Patients.ToListAsync();
        return Results.Ok(patients);
    }

    private static async Task<IResult> GetPatientById(int id, PatientDataDbContext dbContext)
    {
        var patient = await dbContext.Patients.FindAsync(id);
        return patient is not null ? Results.Ok(patient) : Results.NotFound();
    }

    private static async Task<IResult> SearchPatients(
        string? firstName, 
        string? lastName, 
        string? fullName, 
        PatientDataDbContext dbContext)
    {
        var query = dbContext.Patients.AsQueryable();

        if (!string.IsNullOrEmpty(fullName))
        {
            query = ApplyFullNameSearch(query, fullName);
        }
        else
        {
            query = ApplyIndividualNameSearch(query, firstName, lastName);
        }

        var results = await query.ToListAsync();
        return Results.Ok(results);
    }

    private static async Task<IResult> CreatePatient(PatientEntity patient, PatientDataDbContext dbContext)
    {
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync();
        return Results.Created($"/api/patients/{patient.Id}", patient);
    }

    private static IQueryable<PatientEntity> ApplyFullNameSearch(IQueryable<PatientEntity> query, string fullName)
    {
        // Handle full name search - split and search for parts
        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nameParts.Length >= 2)
        {
            var firstPart = nameParts[0];
            var lastPart = nameParts[nameParts.Length - 1];
            return query.Where(p => 
                (p.FirstName.ToLower().Contains(firstPart.ToLower()) && p.LastName.ToLower().Contains(lastPart.ToLower())) ||
                (p.FirstName.ToLower().Contains(lastPart.ToLower()) && p.LastName.ToLower().Contains(firstPart.ToLower())));
        }
        else
        {
            // Single name, search in both first and last name
            return query.Where(p => p.FirstName.ToLower().Contains(fullName.ToLower()) || 
                               p.LastName.ToLower().Contains(fullName.ToLower()));
        }
    }

    private static IQueryable<PatientEntity> ApplyIndividualNameSearch(IQueryable<PatientEntity> query, string? firstName, string? lastName)
    {
        if (!string.IsNullOrEmpty(firstName))
        {
            query = query.Where(p => p.FirstName.ToLower().Contains(firstName.ToLower()));
        }
        if (!string.IsNullOrEmpty(lastName))
        {
            query = query.Where(p => p.LastName.ToLower().Contains(lastName.ToLower()));
        }
        return query;
    }
}