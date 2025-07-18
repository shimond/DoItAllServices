using Microsoft.EntityFrameworkCore;
using PatientHistoryService.DataAccess;
using PatientHistoryService.Models;
using Infra.Messaging;

namespace PatientHistoryService.Endpoints;

public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/history").WithTags("Patient History");

        group.MapGet("/{patientId:int}", GetPatientHistory);
    }

    private static async Task<IResult> GetPatientHistory(
        int patientId, 
        PatientDbContext context, 
        IEventBus eventBus)
    {
        var vitalsHistory = await context.VitalsHistory
            .Where(x => x.PatientId == patientId)
            .ToListAsync();
        
        if (!vitalsHistory.Any())
            return Results.NotFound();

        return Results.Ok(vitalsHistory);
    }

 
}