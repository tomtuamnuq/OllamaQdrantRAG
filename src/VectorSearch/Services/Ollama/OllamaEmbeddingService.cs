using System.Net.Http.Json;
using VectorSearch.Configuration;

namespace VectorSearch.Services.Ollama;

public class OllamaEmbeddingService(IHttpClientFactory httpClientFactory, OllamaConfiguration config)
    : IEmbeddingService
{
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var client = httpClientFactory.CreateClient("ollama");
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null, empty or whitespace.", nameof(text));
        }


        var request = new EmbeddingRequest(config.EmbeddingModel, text);

        var response = await client.PostAsJsonAsync("/api/embeddings", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        return result?.Embedding ?? [];
    }


    private record EmbeddingRequest(string Model, string Prompt);

    private record EmbeddingResponse(float[] Embedding);
}