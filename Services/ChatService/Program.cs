using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.AddOllamaApiClient("ollama-llama3").AddChatClient();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/chat", async (ChatRequest request, IChatClient chatClient) =>
{
    var chatRequest = new ChatMessage(  ChatRole.User, request.Question);
    var response = await chatClient.GetResponseAsync(chatRequest);
    return Results.Ok(new { response = response });
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record ChatRequest(string Question);
