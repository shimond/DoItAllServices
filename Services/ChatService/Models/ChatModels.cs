namespace ChatService.Models;

public record ChatRequest(string Question, List<ConversationMessage>? ConversationHistory = null);

public record ConversationMessage(string Role, string Content);

public class PatientIdentificationResult
{
    public int[]? PatientIds { get; set; }
    public string[]? PatientNames { get; set; }
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