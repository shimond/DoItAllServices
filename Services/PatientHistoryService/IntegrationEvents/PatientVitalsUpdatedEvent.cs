using Infra.Messaging.Models;

namespace PatientHistoryService.IntegrationEvents;
public class PatientVitalsUpdatedEvent : IntegrationEvent
{
    public int PatientId { get; set; }
    public string VitalsData { get; set; }
    public DateTime Timestamp { get; set; }
}