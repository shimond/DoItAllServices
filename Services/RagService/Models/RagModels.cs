namespace RagService.Models;

public record RagUpsertRequest(string PatientId, string Text, float[] Vector, string? PatientName = null);

public record RagQueryRequest(string Query, int TopK = 3);

public record RagPatientQueryRequest(string PatientName, int TopK = 10);