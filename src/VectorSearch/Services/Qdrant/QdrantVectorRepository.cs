using System.Collections.Immutable;
using Grpc.Core;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using VectorSearch.Configuration;

namespace VectorSearch.Services.Qdrant;

public class QdrantVectorRepository(QdrantClient client, QdrantConfiguration configuration) : IVectorRepository
{
    public async Task<bool> IsHealthyAsync()
    {
        var health = await client.HealthAsync();
        return health.HasCommit;
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(
        string collectionName,
        float[] queryVector,
        int limit)
    {
        CheckCollectionName(collectionName);
        CheckVector(queryVector);
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than zero", nameof(limit));
        try 
        {
            var searchResults = await client.SearchAsync(
                collectionName,
                queryVector,
                limit: (ulong)limit);

            return searchResults.Select(result => new SearchResult(
                result.Id.ToString(),
                result.Score,
                result.Payload["text"].StringValue));
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            throw new InvalidOperationException($"Collection {collectionName} does not exist", e);
        }
    }

    private void CheckVector(float[] vector)
    {
        if (vector == null || vector.Length == 0)
            throw new ArgumentException("Query vector cannot be empty", nameof(vector));

        if (vector.Length != (int)configuration.VectorSize)
            throw new ArgumentException($"Query vector must have length {configuration.VectorSize}", nameof(vector));
    }

    private static void CheckCollectionName(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
            throw new ArgumentException("Collection name cannot be empty", nameof(collectionName));
    }

    public async Task UpsertVectorsAsync(string collectionName,
        IEnumerable<(float[] Vector, string Payload)> vectors)
    {
        CheckCollectionName(collectionName);
        var vectorsList = vectors.ToImmutableList();
        if (!vectorsList.Any())
            throw new ArgumentException("Vectors collection cannot be empty", nameof(vectors));
        foreach (var (vector, payload) in vectorsList)
        {
            CheckVector(vector);
            if (string.IsNullOrWhiteSpace(payload))
                throw new ArgumentException("Payload cannot be empty");
        }
        try 
        {
            var points = vectorsList.Select((item, index) => new PointStruct
            {
                Id = Guid.NewGuid(),
                Vectors = item.Vector,
                Payload = { ["text"] = item.Payload }
            }).ToImmutableList();

            await client.UpsertAsync(collectionName, points);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            throw new InvalidOperationException($"Collection {collectionName} does not exist", e);
        }
    }

    public async Task EnsureCollectionExistsAsync(string collectionName)
    {
        CheckCollectionName(collectionName);
        var exists = await client.GetCollectionInfoAsync(collectionName)
            .ContinueWith(t => !t.IsFaulted);

        if (!exists)
        {
            if (configuration.VectorSize <= 0)
                throw new ArgumentException("Vector size must be greater than zero");
            await client.CreateCollectionAsync(collectionName,
                            new VectorParams
                            {
                                Size = configuration.VectorSize,
                                Distance = Distance.Cosine
                            }
                        );
        }
            
    }
}