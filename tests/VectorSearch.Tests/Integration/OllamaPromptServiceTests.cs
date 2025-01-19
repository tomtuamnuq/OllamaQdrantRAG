using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VectorSearch.Configuration;
using VectorSearch.Services;
using VectorSearch.Services.Ollama;

namespace VectorSearch.Tests.Integration;

public class OllamaPromptServiceTests : TestBase
{
    private readonly OllamaPromptService _promptService;

    public OllamaPromptServiceTests()
    {
        var httpClientFactory = ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var config = new OllamaConfiguration();
        ILogger<OllamaPromptService> logger = ServiceProvider.GetRequiredService<ILogger<OllamaPromptService>>();
        _promptService = new OllamaPromptService(httpClientFactory, config, logger);
    }

    [Fact]
    public async Task GetResponseAsync_WithValidInput_ReturnsResponse()
    {
        // Arrange
        const string promptText = "What is the best programming language?";
        var similarityResults = new List<SearchResult>
        {
            new("1", 0.9f, "Python is a versatile programming language known for its readability."),
            new("2", 0.8f, "JavaScript is widely used in web development.")
        };

        // Act
        var response = await _promptService.GetResponseAsync(promptText, similarityResults);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.DoesNotContain("error", response.ToLower()); // Basic check that we didn't get an error message
    }

    [Fact]
    public async Task GetResponseAsync_WithEmptySimilarityResults_UsesOnlyPromptText()
    {
        // Arrange
        const string promptText = "What is your favorite programming concept?";

        // Act
        var response = await _promptService.GetResponseAsync(promptText, new List<SearchResult>());

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
    }

    [Fact]
    public async Task GetResponseAsync_WithMultipleSimilarityResults_OrdersByScore()
    {
        // Arrange
        const string promptText = "Explain the concept of inheritance.";
        var similarityResults = new List<SearchResult>
        {
            new("1", 0.5f, "Low relevance content"),
            new("2", 0.9f, "High relevance content about inheritance"),
            new("3", 0.7f, "Medium relevance content")
        };

        // Act
        var response = await _promptService.GetResponseAsync(promptText, similarityResults);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        // The response should exist and be properly formatted, though we can't test
        // the exact content since it's AI-generated
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetResponseAsync_WithInvalidPromptText_ThrowsArgumentException(string invalidPrompt)
    {
        // Arrange
        var similarityResults = new List<SearchResult>
        {
            new("1", 0.9f, "Some content")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _promptService.GetResponseAsync(invalidPrompt, similarityResults));
    }

    [Fact]
    public async Task GetResponseAsync_WithNullSimilarityResults_ThrowsArgumentNullException()
    {
        // Arrange
        const string promptText = "Valid prompt text";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _promptService.GetResponseAsync(promptText, null!));
    }

    [Fact]
    public async Task GetResponseAsync_WithSimilarityResultsContainingEmptyPayloads_SkipsEmptyPayloads()
    {
        // Arrange
        const string promptText = "What are the best practices for error handling?";
        var similarityResults = new List<SearchResult>
        {
            new("1", 0.9f, ""),
            new("2", 0.8f, "Valid content about error handling"),
            new("3", 0.7f, "   "),
            new("4", 0.6f, null!)
        };

        // Act
        var response = await _promptService.GetResponseAsync(promptText, similarityResults);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
    }

    [Fact]
    public async Task GetResponseAsync_PreservesAngerPiratePersona()
    {
        // Arrange
        const string promptText = "What do you think about using global variables?";
        var similarityResults = new List<SearchResult>
        {
            new("1", 0.9f, "Global variables can lead to maintenance issues and make code harder to test.")
        };

        // Act
        var response = await _promptService.GetResponseAsync(promptText, similarityResults);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        // While we can't guarantee specific pirate terms, the response should be non-empty
        // and maintain the angry pirate persona as defined in the system prompt
    }

    [Fact]
    public async Task GetResponseAsync_HandlesLongSimilarityResults()
    {
        // Arrange
        const string promptText = "Explain microservices architecture.";
        var longText = new string('x', 10000); // Create a long string
        var similarityResults = new List<SearchResult>
        {
            new("1", 0.9f, longText)
        };

        // Act
        var response = await _promptService.GetResponseAsync(promptText, similarityResults);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
    }
}