using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using VectorSearch.Configuration;
using VectorSearch.Services.Qdrant;

namespace VectorSearch.Tests.Integration;

public class QdrantVectorRepositoryTests : TestBase
{
    private readonly QdrantVectorRepository _vectorRepository;
    private readonly QdrantClient _client;
    private readonly string _testCollection = $"test_collection_{Guid.NewGuid():N}";

    public QdrantVectorRepositoryTests()
    {
        _client = ServiceProvider.GetRequiredService<QdrantClient>();
        var config = new QdrantConfiguration(VectorSize:3);
        _vectorRepository = new QdrantVectorRepository(_client, config);
    }

    public override async Task DisposeAsync()
    {
        try
        {
            await _client.DeleteCollectionAsync(_testCollection);
        }
        catch
        {
            // Collection might not exist, ignore
        }
        await base.DisposeAsync();
    }

    [Fact]
    public async Task IsHealthyAsync_ReturnsTrue_WhenQdrantIsRunning()
    {
        // Act
        var isHealthy = await _vectorRepository.IsHealthyAsync();

        // Assert
        Assert.True(isHealthy);
    }

    [Fact]
    public async Task EnsureCollectionExistsAsync_CreatesNewCollection_WhenDoesNotExist()
    {
        // Act
        await _vectorRepository.EnsureCollectionExistsAsync(_testCollection);

        // Assert
        var collectionInfo = await _client.GetCollectionInfoAsync(_testCollection);
        Assert.NotNull(collectionInfo);
        Assert.Equal(CollectionStatus.Green, collectionInfo.Status);
    }

    [Fact]
    public async Task EnsureCollectionExistsAsync_DoesNotThrow_WhenCollectionAlreadyExists()
    {
        // Arrange
        await _vectorRepository.EnsureCollectionExistsAsync(_testCollection);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            _vectorRepository.EnsureCollectionExistsAsync(_testCollection));
        Assert.Null(exception);
    }

    [Fact]
    public async Task UpsertVectorsAsync_SuccessfullyStoresVectors()
    {
        // Arrange
        await _vectorRepository.EnsureCollectionExistsAsync(_testCollection);
        List<(float[], string)> vectors = [
            ([ 1.0f, 0.0f, 0.0f ], "test payload 1"),
            ([ 0.0f, 1.0f, 0.0f ], "test payload 2")
        ];

        // Act
        await _vectorRepository.UpsertVectorsAsync(_testCollection, vectors);

        // Assert
        var searchResults = await _vectorRepository.SearchAsync(
            _testCollection,
            [1.0f, 0.0f, 0.0f],
            limit: 2);
        
        var resultsList = searchResults.ToList();
        Assert.Equal(2, resultsList.Count);
        Assert.Contains(resultsList, r => r.Payload == "test payload 1");
        Assert.Contains(resultsList, r => r.Payload == "test payload 2");
    }

    [Fact]
    public async Task SearchAsync_ReturnsOrderedResults_BasedOnSimilarity()
    {
        // Arrange
        await _vectorRepository.EnsureCollectionExistsAsync(_testCollection);
         List<(float[], string)> vectors = [
            ([1.0f, 0.0f, 0.0f], "closest"),
            ([0.7f, 0.7f, 0.0f], "medium"),
            ([ 0.0f, 1.0f, 0.0f ], "furthest")
        ];
        await _vectorRepository.UpsertVectorsAsync(_testCollection, vectors);

        // Act
        var searchResults = await _vectorRepository.SearchAsync(
            _testCollection,
            [1.0f, 0.0f, 0.0f],
            limit: 3);

        // Assert
        var resultsList = searchResults.ToList();
        Assert.Equal(3, resultsList.Count);
        Assert.Equal("closest", resultsList[0].Payload);
        Assert.True(resultsList[0].Score > resultsList[1].Score);
        Assert.True(resultsList[1].Score > resultsList[2].Score);
    }

    [Fact]
    public async Task SearchAsync_RespectsLimit()
    {
        // Arrange
        await _vectorRepository.EnsureCollectionExistsAsync(_testCollection);
        List<(float[], string)> vectors = [
            ([ 1.0f, 0.0f, 0.0f ], "test payload 1"),
            ([ 0.0f, 1.0f, 0.0f ], "test payload 2")
        ];
        await _vectorRepository.UpsertVectorsAsync(_testCollection, vectors);

        // Act
        var searchResults = await _vectorRepository.SearchAsync(
            _testCollection,
            [1.0f, 0.0f, 0.0f],
            limit: 1);

        // Assert
        Assert.Single(searchResults);
    }

    [Fact]
    public async Task SearchAsync_WithNonExistentCollection_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _vectorRepository.SearchAsync(
                "non_existent_collection",
                [1.0f, 0.0f, 0.0f],
                limit: 10));
    }

    [Fact]
    public async Task UpsertVectorsAsync_WithEmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        await _vectorRepository.EnsureCollectionExistsAsync(_testCollection);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _vectorRepository.UpsertVectorsAsync(_testCollection, Array.Empty<(float[] Vector, string Payload)>()));
    }

    [Fact]
    public async Task UpsertVectorsAsync_WithInvalidVectorSize_ThrowsArgumentException()
    {
        // Arrange
        await _vectorRepository.EnsureCollectionExistsAsync(_testCollection);
        List<(float[], string)> vectors = [
            ([ 1.0f, 0.0f ], "wrong size vector")
        ];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _vectorRepository.UpsertVectorsAsync(_testCollection, vectors));
    }

    [Fact]
    public async Task SearchAsync_WithInvalidVectorSize_ThrowsArgumentException()
    {
        // Arrange
        await _vectorRepository.EnsureCollectionExistsAsync(_testCollection);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _vectorRepository.SearchAsync(
                _testCollection,
                [1.0f], // Wrong size vector
                limit: 10));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SearchAsync_WithInvalidCollectionName_ThrowsArgumentException(string collectionName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _vectorRepository.SearchAsync(
                collectionName,
                [1.0f, 0.0f, 0.0f],
                limit: 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SearchAsync_WithInvalidLimit_ThrowsArgumentException(int limit)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _vectorRepository.SearchAsync(
                _testCollection,
                [1.0f, 0.0f, 0.0f],
                limit));
    }
}