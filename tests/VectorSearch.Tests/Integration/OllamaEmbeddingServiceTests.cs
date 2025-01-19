using Microsoft.Extensions.DependencyInjection;
using VectorSearch.Configuration;
using VectorSearch.Services.Ollama;

namespace VectorSearch.Tests.Integration;

public class OllamaEmbeddingServiceTests : TestBase {
    private readonly OllamaEmbeddingService _embeddingService;

    public OllamaEmbeddingServiceTests()
    {
        var httpClientFactory = ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var config = new OllamaConfiguration();
        _embeddingService = new OllamaEmbeddingService(httpClientFactory, config);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithValidText_ReturnsEmbeddingVector()
    {
        // Arrange
        const string text = "This is a test text for embedding generation";

        // Act
        var embedding = await _embeddingService.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(embedding);
        Assert.Equal(1024, embedding.Length); // Based on QdrantConfiguration VectorSize
        Assert.Contains(embedding, v => v != 0); // Verify we have non-zero values
    }


    [Fact]
    public async Task GetEmbeddingAsync_WithLongText_ReturnsValidEmbedding()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("This is a long text that will be repeated multiple times to test the embedding service with longer inputs", 10));

        // Act
        var embedding = await _embeddingService.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(embedding);
        Assert.Equal(1024, embedding.Length);
        Assert.Contains(embedding, v => v != 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetEmbeddingAsync_WithInvalidInput_ThrowsException(string invalidText)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _embeddingService.GetEmbeddingAsync(invalidText));
    }

    [Fact]
    public async Task GetEmbeddingAsync_ConsistentResults_ForSameInput()
    {
        // Arrange
        const string text = "This is a test text";

        // Act
        var embedding1 = await _embeddingService.GetEmbeddingAsync(text);
        var embedding2 = await _embeddingService.GetEmbeddingAsync(text);

        // Assert
        Assert.Equal(embedding1.Length, embedding2.Length);
        for (var i = 0; i < embedding1.Length; i++)
        {
            Assert.Equal(embedding1[i], embedding2[i], precision: 7);
        }
    }
}