using Infra.Messaging;
using PatientHistoryService.DataAccess;
using PatientHistoryService.DataEntities;
using PatientHistoryService.IntegrationEvents;
using System.Text.Json;

namespace PatientHistoryService
{
    public class PatientHistoryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventBus _eventBus;
        private readonly ILogger<PatientHistoryService> _logger;

        public PatientHistoryService(IServiceProvider serviceProvider, IEventBus eventBus, ILogger<PatientHistoryService> logger)
        {
            _serviceProvider = serviceProvider;
            _eventBus = eventBus;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var tmpContext = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
                tmpContext.Database.EnsureCreated();
            }

            _eventBus.Subscribe<PatientVitalsUpdatedEvent>(async (vitalsEvent) =>
            {
                using var subScope = _serviceProvider.CreateScope();
                var context = subScope.ServiceProvider.GetRequiredService<PatientDbContext>();
                
                // Serialize VitalsData to JSON
                string vitalsDataJson = JsonSerializer.Serialize(vitalsEvent.VitalsData);
                
                // Save vitals to database
                context.VitalsHistory.Add(new VitalsHistory
                {
                    PatientId = vitalsEvent.PatientId,
                    VitalsDataJson = vitalsDataJson,
                    RecordedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync(stoppingToken);
            });

            return Task.CompletedTask;
        }
    }
}