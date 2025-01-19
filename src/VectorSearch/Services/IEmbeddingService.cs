namespace VectorSearch.Services;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
}