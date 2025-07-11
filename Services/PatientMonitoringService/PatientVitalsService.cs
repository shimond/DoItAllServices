using Infra.Messaging;
using PatientMonitoringService.IntegrationEvents;
using PatientMonitoringService.Models;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Tasks;

namespace PatientMonitoringService.Services
{
    public interface IPatientVitalsService
    {
        Task StorePatientVitalsAsync(int patientId, VitalsData vitalsData);
        Task<VitalsData?> GetPatientVitalsAsync(int patientId);
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

        public async Task StorePatientVitalsAsync(int patientId, VitalsData vitalsData)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(GetRedisKey(patientId), JsonSerializer.Serialize(vitalsData));
            var vitalsEvent = new PatientVitalsUpdatedEvent
            {
                PatientId = patientId,
                VitalsData = vitalsData,
                Timestamp = DateTime.UtcNow
            };

            _eventBus.Publish(vitalsEvent);
        }

        public async Task<VitalsData?> GetPatientVitalsAsync(int patientId)
        {
            var db = _redis.GetDatabase();
            var res = await db.StringGetAsync(GetRedisKey(patientId));
            return res.HasValue 
                ? JsonSerializer.Deserialize<VitalsData>(res) 
                : null;
        }

        private string GetRedisKey(int patientId)
        {
            return $"patient:vitals:{patientId}";
        }
    }
}
