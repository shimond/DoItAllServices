using Infra.Messaging.Models;

namespace PatientMonitoringService.IntegrationEvents;
public class PatientVitalsUpdatedEvent : IntegrationEvent
{
    public int PatientId { get; set; }
    public string VitalsData { get; set; }
    public DateTime Timestamp { get; set; }
}