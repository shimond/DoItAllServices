namespace PatientHistoryService.DataEntities;
public class VitalsHistory
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string VitalsData { get; set; }
    public DateTime RecordedAt { get; set; }
}

