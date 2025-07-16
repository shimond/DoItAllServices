using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();

builder.AddOpenAIClient("openai")
        .AddChatClient()
        .UseOpenTelemetry();

//var ollma = builder.AddOllamaApiClient("ollama-phi3");
//ollma.AddChatClient();

var ollmaEmbedding = builder.AddOllamaApiClient("ollama-nomic-embed-text");
ollmaEmbedding.AddEmbeddingGenerator();

builder.Services.AddHttpClient("ragservice", c =>
{
    c.BaseAddress = new Uri("http://ragservice");
});

builder.Services.AddHttpClient("patienthistoryservice", c =>
{
    c.BaseAddress = new Uri("http://patienthistoryservice");
});


var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/chat", async (ChatRequest request, IChatClient chatClient, IEmbeddingGenerator<string, Embedding<float>> a, IHttpClientFactory httpClientFactory) =>
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
        // Extract patient ID considering conversation context
        var patientIdPrompt = $"Extract the patient ID number one or more from this conversation. Look at the conversation history and current question. If a patient ID was mentioned in the conversation history, use that ID. If a new patient ID is mentioned in the current question, use the new one. If no patient ID is found anywhere, return [0]. Only return the array with numbers (not string) with all the id you think we can use, nothing else.\n\nConversation history:\n{conversationContext}\n\nCurrent question: {request.Question}";
        var patientIdRequest = new ChatMessage(ChatRole.User, patientIdPrompt);
        var patientIdResponse = await chatClient.GetResponseAsync(patientIdRequest);
        var patientIds = JsonSerializer.Deserialize<int[]>(patientIdResponse.Text);

        var context = "";
        foreach (var patientId in patientIds)
        {
            var historyClient = httpClientFactory.CreateClient("patienthistoryservice");
            var vitals = await historyClient.GetStringAsync($"/history/{patientId}");
            context += $"\nLatest vitals for patient {patientId}: {vitals}";
        }

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
    else
    {
        // RAG flow with conversation context
        var contextualQuestion = string.IsNullOrEmpty(conversationContext)
            ? request.Question
            : $"Conversation context:\n{conversationContext}\n\nCurrent question: {request.Question}";

        var result = await a.GenerateAsync(new[] { contextualQuestion }, cancellationToken: default);
        var ragClient = httpClientFactory.CreateClient("ragservice");
        var ragReq = new
        {
            Query = JsonSerializer.Serialize(result[0].Vector),
            TopK = 10
        };
        var res = await ragClient.PostAsJsonAsync("/rag/query", ragReq);
        var contextArray = await res.Content.ReadFromJsonAsync<string[]>();
        var ragContext = string.Join("\n", contextArray ?? Array.Empty<string>());

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
});

string BuildConversationContext(List<ConversationMessage>? conversationHistory)
{
    if (conversationHistory == null || conversationHistory.Count == 0)
        return string.Empty;

    var context = string.Join("\n", conversationHistory.Select(msg =>
        $"{(msg.Role == "user" ? "User" : "Assistant")}: {msg.Content}"));

    return context;
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record ChatRequest(string Question, List<ConversationMessage>? ConversationHistory = null);

public record ConversationMessage(string Role, string Content);
