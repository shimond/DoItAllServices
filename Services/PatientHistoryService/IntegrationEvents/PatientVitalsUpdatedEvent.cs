using Infra.Messaging.Models;
using PatientHistoryService.Models;

namespace PatientHistoryService.IntegrationEvents;
public class PatientVitalsUpdatedEvent : IntegrationEvent
{
    public int PatientId { get; set; }
    public VitalsData VitalsData { get; set; }
    public DateTime Timestamp { get; set; }
}