namespace VectorSearch.Configuration;

public record OllamaConfiguration(
    string BaseUrl = "http://localhost:11434",
    string EmbeddingModel = "snowflake-arctic-embed2",
    string GenerativeModel = "mistral");