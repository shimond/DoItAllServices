using PatientMonitoringService.Models;

namespace PatientMonitoringService.Models;

public class VitalsRequest
{
    public int PatientId { get; set; }
    public VitalsData VitalsData { get; set; } = new();
}

public class PatientInfo
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class OllamaEmbeddingResponse
{
    public float[] embedding { get; set; } = Array.Empty<float>();
}