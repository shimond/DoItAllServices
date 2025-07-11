using PatientHistoryService.Models;

namespace PatientHistoryService.DataEntities;
public class VitalsHistory
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string VitalsDataJson { get; set; } // Serialized VitalsData
    public DateTime RecordedAt { get; set; }
}

