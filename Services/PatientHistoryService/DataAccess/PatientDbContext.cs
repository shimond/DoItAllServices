
using Microsoft.EntityFrameworkCore;
using PatientHistoryService.DataEntities;

namespace PatientHistoryService.DataAccess;
 public class PatientDbContext : DbContext
{
    public PatientDbContext(DbContextOptions<PatientDbContext> options) : base(options)
    {
    }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<VitalsHistory> VitalsHistory { get; set; }
}

