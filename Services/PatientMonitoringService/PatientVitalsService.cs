using Infra.Messaging;
using PatientMonitoringService.IntegrationEvents;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace PatientMonitoringService.Services
{
    public interface IPatientVitalsService
    {
        Task StorePatientVitalsAsync(int patientId, string vitalsData);
        Task<string?> GetPatientVitalsAsync(int patientId);
    }

    public class PatientVitalsService : IPatientVitalsService
    {
        private readonly IEventBus _eventBus;
        private readonly IConnectionMultiplexer _redis;

        public PatientVitalsService(IConnectionMultiplexer redis, IEventBus eventBus)
        {
            _eventBus = eventBus;
            _redis = redis;
        }

        public async Task StorePatientVitalsAsync(int patientId, string vitalsData)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(GetRedisKey(patientId), vitalsData);
            var vitalsEvent = new PatientVitalsUpdatedEvent
            {
                PatientId = patientId,
                VitalsData = vitalsData,
                Timestamp = DateTime.UtcNow
            };

            _eventBus.Publish(vitalsEvent);
        }

        public async Task<string?> GetPatientVitalsAsync(int patientId)
        {
            var db = _redis.GetDatabase();
            return await db.StringGetAsync(GetRedisKey(patientId));
        }

        private string GetRedisKey(int patientId)
        {
            return $"patient:vitals:{patientId}";
        }
    }
}
