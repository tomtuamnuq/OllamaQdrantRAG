namespace VectorSearch.Services;

public interface IVectorRepository
{
    Task<bool> IsHealthyAsync();
    Task EnsureCollectionExistsAsync(string collectionName);

    Task UpsertVectorsAsync(string collectionName,
        IEnumerable<(float[] Vector, string Payload)> vectors);

    Task<IEnumerable<SearchResult>> SearchAsync(string collectionName, float[] queryVector, int limit = 10);
}