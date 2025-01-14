using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PatientDataAPI.DataEntities;
namespace PatientDataAPI.DataContext;


public class PatientDataDbContext : DbContext
{
    public PatientDataDbContext(DbContextOptions<PatientDataDbContext> options) : base(options)
    {
    }

    public DbSet<PatientEntity> Patients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all DateTime properties to be stored in UTC
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                        v => v.ToUniversalTime(),  // Convert to UTC when saving
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));  // Convert to UTC when reading
                }
            }
        }
    }

}