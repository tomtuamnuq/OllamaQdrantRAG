using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;
using VectorSearch.Configuration;
using VectorSearch.Services;
using VectorSearch.Services.FileReader;
using VectorSearch.Services.Ollama;
using VectorSearch.Services.Qdrant;

namespace VectorSearch;

public static class ServiceExtensions
{
    public static void RegisterServices(this IServiceCollection services)
    {
        // Configuration
        services.AddSingleton<QdrantConfiguration>();
        services.AddSingleton<OllamaConfiguration>();

        // Clients
        services.AddHttpClient("ollama", (provider, client) =>
        {
            var config = provider.GetRequiredService<OllamaConfiguration>();
            client.BaseAddress = new Uri(config.BaseUrl);
        });

        services.AddSingleton<QdrantClient>(provider =>
        {
            var config = provider.GetRequiredService<QdrantConfiguration>();
            return new QdrantClient(config.Host, config.Port);
        });

        // Services
        services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
        services.AddSingleton<IVectorRepository, QdrantVectorRepository>();
        services.AddSingleton<IFileReader>(provider => new FileReaderFactory(
            new Dictionary<string, IFileReader>
            {
                { ".txt", new TxtFileReader() },
                { ".pdf", new PdfFileReader() }
            }));
        services.AddSingleton<IPromptService, OllamaPromptService>();
        // Main application
        services.AddSingleton<App>();
    }
}