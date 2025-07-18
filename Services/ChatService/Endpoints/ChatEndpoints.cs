using Microsoft.Extensions.AI;
using System.Text.Json;
using ChatService.Models;

namespace ChatService.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        app.MapPost("/chat", async (ChatRequest request, IChatClient chatClient, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, IHttpClientFactory httpClientFactory) =>
        {
            // Build conversation context from previous messages
            var conversationContext = BuildConversationContext(request.ConversationHistory);

            // Use LLM to classify if the question is a structured analytics query, considering conversation context
            var classificationPrompt = $"Classify the following question as either 'structured' (if it can be answered by querying structured patient vitals data directly, e.g. latest, max, min, average, specific patient, etc.) or 'rag' (if it requires semantic search or unstructured context). Only answer 'structured' or 'rag'.\n\nConversation context:\n{conversationContext}\n\nCurrent question: {request.Question}";
            var classificationRequest = new ChatMessage(ChatRole.User, classificationPrompt);

            var classificationResponse = await chatClient.GetResponseAsync(classificationRequest);
            var classification = classificationResponse.ToString().Trim().ToLower();

            if (classification.StartsWith("structured"))
            {
                return await HandleStructuredQuery(request, chatClient, httpClientFactory, conversationContext);
            }
            else
            {
                return await HandleRagQuery(request, chatClient, embeddingGenerator, httpClientFactory, conversationContext);
            }
        });
    }

    private static async Task<IResult> HandleStructuredQuery(ChatRequest request, IChatClient chatClient, IHttpClientFactory httpClientFactory, string conversationContext)
    {
        // Enhanced patient identification - extract both IDs and names
        var jsonFormat = """{"patientIds": [1, 2], "patientNames": ["John Smith", "Sarah Johnson"]}""";
        var patientIdentificationPrompt = $"""
Given the following conversation history and current question, extract patient identifiers (both IDs and names).

Rules:
- Look for patient ID numbers (integers) AND patient names (e.g., "John Smith", "Sarah Johnson")
- If one or more patient IDs were mentioned in the conversation history, include them.
- If new patient IDs or names are mentioned in the current question, prioritize these new ones.
- If no patient identifiers are found anywhere, return empty arrays.
- Return JSON in this exact format: {jsonFormat}

Conversation history:
{conversationContext}

Current question:
{request.Question}
""";
        
        var patientIdRequest = new ChatMessage(ChatRole.User, patientIdentificationPrompt);
        var patientIdResponse = await chatClient.GetResponseAsync(patientIdRequest);
        
        var patientInfo = JsonSerializer.Deserialize<PatientIdentificationResult>(patientIdResponse.Text) ?? new PatientIdentificationResult();
        
        // Resolve patient names to IDs if needed
        var resolvedPatientIds = new List<int>(patientInfo.PatientIds ?? []);
        
        if (patientInfo.PatientNames?.Any() == true)
        {
            var patientDataClient = httpClientFactory.CreateClient("patientdataapi");
            foreach (var name in patientInfo.PatientNames)
            {
                try
                {
                    var searchResponse = await patientDataClient.GetFromJsonAsync<PatientInfo[]>($"/api/patients/search?fullName={Uri.EscapeDataString(name)}");
                    if (searchResponse?.Any() == true)
                    {
                        resolvedPatientIds.AddRange(searchResponse.Select(p => p.Id));
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing
                    Console.WriteLine($"Error resolving patient name '{name}': {ex.Message}");
                }
            }
        }

        // Remove duplicates
        var finalPatientIds = resolvedPatientIds.Distinct().ToArray();
        
        // If no patients found, default to [0]
        if (!finalPatientIds.Any())
        {
            finalPatientIds = [0];
        }

        var context = "";
        foreach (var patientId in finalPatientIds.Where(id => id > 0))
        {
            var historyClient = httpClientFactory.CreateClient("patienthistoryservice");
            try
            {
                var vitals = await historyClient.GetStringAsync($"/history/{patientId}");
                context += $"\nLatest vitals for patient {patientId}: {vitals}";
            }
            catch
            {
                context += $"\nNo vitals data found for patient {patientId}";
            }
        }

        context += "\nAlways you return a datetime please return it in human format";
        // Include conversation context in the prompt
        var prompt = $"Conversation context:\n{conversationContext}\n\nCurrent context:\n{context}\n\nCurrent question: {request.Question}";
        var chatRequest2 = new ChatMessage(ChatRole.User, prompt);
        var response = await chatClient.GetResponseAsync(chatRequest2);

        // Create response message for conversation history
        var responseMessage = new ConversationMessage("assistant", response.ToString());
        var currentMessage = new ConversationMessage("user", request.Question);

        return Results.Ok(new
        {
            response = response.ToString(),
            conversationMessage = responseMessage,
            userMessage = currentMessage
        });
    }

    private static async Task<IResult> HandleRagQuery(ChatRequest request, IChatClient chatClient, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, IHttpClientFactory httpClientFactory, string conversationContext)
    {
        // Enhanced RAG flow - try patient name search first, then general semantic search
        var contextualQuestion = string.IsNullOrEmpty(conversationContext)
            ? request.Question
            : $"Conversation context:\n{conversationContext}\n\nCurrent question: {request.Question}";

        // Try to extract patient names for targeted RAG search
        var nameExtractionPrompt = $"Extract any patient names mentioned in this question. Return only the names separated by commas, or 'NONE' if no names found: {request.Question}";
        var nameRequest = new ChatMessage(ChatRole.User, nameExtractionPrompt);
        var nameResponse = await chatClient.GetResponseAsync(nameRequest);
        var extractedNames = nameResponse.ToString().Trim();

        string ragContext = "";
        var ragClient = httpClientFactory.CreateClient("ragservice");

        if (!extractedNames.Equals("NONE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(extractedNames))
        {
            // Try patient-specific RAG search first
            var names = extractedNames.Split(',').Select(n => n.Trim()).Where(n => !string.IsNullOrEmpty(n));
            foreach (var name in names)
            {
                try
                {
                    var patientRagReq = new { PatientName = name, TopK = 5 };
                    var patientRes = await ragClient.PostAsJsonAsync("/rag/query-by-patient", patientRagReq);
                    if (patientRes.IsSuccessStatusCode)
                    {
                        var patientContextArray = await patientRes.Content.ReadFromJsonAsync<string[]>();
                        if (patientContextArray?.Any() == true)
                        {
                            ragContext += string.Join("\n", patientContextArray) + "\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in patient-specific RAG search for '{name}': {ex.Message}");
                }
            }
        }

        // If no patient-specific results or as fallback, do general semantic search
        if (string.IsNullOrWhiteSpace(ragContext))
        {
            var result = await embeddingGenerator.GenerateAsync(new[] { contextualQuestion }, cancellationToken: default);
            var ragReq = new
            {
                Query = JsonSerializer.Serialize(result[0].Vector),
                TopK = 10
            };
            var res = await ragClient.PostAsJsonAsync("/rag/query", ragReq);
            var contextArray = await res.Content.ReadFromJsonAsync<string[]>();
            ragContext = string.Join("\n", contextArray ?? Array.Empty<string>());
        }

        var prompt = $"Conversation context:\n{conversationContext}\n\nRAG context:\n{ragContext}\n\nCurrent question: {request.Question}";
        var chatRequest2 = new ChatMessage(ChatRole.User, prompt);
        var response = await chatClient.GetResponseAsync(chatRequest2);

        // Create response message for conversation history
        var responseMessage = new ConversationMessage("assistant", response.ToString());
        var currentMessage = new ConversationMessage("user", request.Question);

        return Results.Ok(new
        {
            response = response.ToString(),
            conversationMessage = responseMessage,
            userMessage = currentMessage
        });
    }

    private static string BuildConversationContext(List<ConversationMessage>? conversationHistory)
    {
        if (conversationHistory == null || conversationHistory.Count == 0)
            return string.Empty;

        var context = string.Join("\n", conversationHistory.Select(msg =>
            $"{(msg.Role == "user" ? "User" : "Assistant")}: {msg.Content}"));

        return context;
    }
}