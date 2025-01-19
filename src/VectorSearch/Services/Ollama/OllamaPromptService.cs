using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VectorSearch.Configuration;

namespace VectorSearch.Services.Ollama;

public class OllamaPromptService(
    IHttpClientFactory httpClientFactory,
    OllamaConfiguration config,
    ILogger<OllamaPromptService> logger)
    : IPromptService
{
    private const string SystemPrompt = """
                                        You are an angry pirate tech expert with strong opinions! Key traits:
                                        - You love technology, programming, and elegant solutions
                                        - You get irritated by bad code and inefficient systems
                                        - You use pirate slang and expressions frequently
                                        - You're direct and don't sugarcoat your opinions
                                        - Despite your gruff exterior, you provide accurate and helpful technical information
                                        - You refer to all technologies as if they were ships or treasures
                                        """;

    public async Task<string> GetResponseAsync(string promptText, IEnumerable<SearchResult> similarityResults)
    {
        if (string.IsNullOrWhiteSpace(promptText))
        {
            throw new ArgumentException("Prompt text cannot be null, empty or whitespace.", nameof(promptText));
        }

        if (similarityResults == null)
        {
            throw new ArgumentNullException(nameof(similarityResults), "Similarity results cannot be null.");
        }

        var client = httpClientFactory.CreateClient("ollama");

        var userPrompt = BuildPrompt(promptText, similarityResults);
        logger.LogInformation("User Prompt:\n{UserPrompt}", userPrompt);

        try
        {
            var request = new ChatRequest(
                config.GenerativeModel,
                [
                    new Message("system", SystemPrompt),
                    new Message("user", userPrompt)
                ]);

            var response = await client.PostAsJsonAsync("/api/chat", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
            
            if (result?.Message?.Content == null)
            {
                throw new InvalidOperationException("Received empty or invalid response from Ollama service.");
            }

            var aiAnswer = result.Message.Content.Trim();
            if (string.IsNullOrWhiteSpace(aiAnswer))
            {
                throw new InvalidOperationException("Received empty response from Ollama service.");
            }

            logger.LogInformation("AI Response:\n{Answer}", aiAnswer);
            return aiAnswer;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to communicate with Ollama service.");
            throw new InvalidOperationException("Failed to communicate with Ollama service.", ex);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse response from Ollama service.");
            throw new InvalidOperationException("Failed to parse response from Ollama service.", ex);
        }
    }

    private string BuildPrompt(string promptText, IEnumerable<SearchResult> results)
    {
        var resultsList = results.ToList();
        var contextBuilder = new StringBuilder();

        if (resultsList.Count <= 0)
        {
            logger.LogWarning("No similarity results provided for context enrichment.");
            return promptText;
        }

        var orderedResults = resultsList
            .Where(r => !string.IsNullOrWhiteSpace(r.Payload))
            .OrderByDescending(r => r.Score)
            .ToList();

        if (orderedResults.Count <= 0)
        {
            logger.LogWarning("All similarity results were empty or invalid.");
            return promptText;
        }

        foreach (var result in orderedResults)
        {
            contextBuilder.AppendLine($"Relevance Score: {result.Score:P1}");
            contextBuilder.AppendLine("Content:");
            contextBuilder.AppendLine(result.Payload.Trim());
            contextBuilder.AppendLine("---");
        }

        var context = contextBuilder.ToString().TrimEnd();
        return $"""
                Based on the following reference information:

                {context}

                Answer this question from your perspective:
                {promptText}
                """;
    }

    private record Message(string Role, string Content);

    private record ChatRequest(string Model, Message[] Messages, bool Stream = false);

    private record ChatResponse(Message Message);
}