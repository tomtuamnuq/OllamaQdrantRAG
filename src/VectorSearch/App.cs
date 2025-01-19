using System.Collections.Immutable;
using System.CommandLine;
using Microsoft.Extensions.Logging;
using VectorSearch.Services;
using VectorSearch.Services.FileReader;

namespace VectorSearch;

public class App(
    IFileReader fileReader,
    IEmbeddingService embeddingService,
    IVectorRepository vectorRepository,
    IPromptService promptService,
    ILogger<App> logger)
{
    public async Task RunAsync(string[] args)
    {
        var rootCommand = new RootCommand("Vector Search Application");
        var textOption = new Option<string>("--text", "The text to use.");
        var collectionOption = new Option<string>("--collection", "The collection to use.");
        var limitOption = new Option<int>("--limit", description: "The limit number of query results.",
            getDefaultValue: () => 3);
        var searchCommand = new Command("search", "Search Command");
        searchCommand.SetHandler(async
                (collection, text, limit) =>
            {
                var queryEmbedding = await embeddingService.GetEmbeddingAsync(text);
                var results = await vectorRepository.SearchAsync(collection, queryEmbedding, limit);

                Console.WriteLine("\nSearch Results:");
                Console.WriteLine("---------------");

                foreach (var result in results)
                {
                    Console.WriteLine($"\nScore: {result.Score:F3}");
                    Console.WriteLine($"Text: {result.Payload}");
                }
            },
            collectionOption, textOption, limitOption);

        searchCommand.AddOption(textOption);
        searchCommand.AddOption(collectionOption);
        searchCommand.AddOption(limitOption);

        var filePathOption = new Option<string>("--file", "The path to the text file to process.");
        var insertCommand = new Command("insert", "Insert Command");
        insertCommand.SetHandler(async
                (collection, text, filePath) =>
            {
                await EnsureCollectionExists(collection);
                if (string.IsNullOrEmpty(filePath))
                {
                    if (string.IsNullOrEmpty(text))
                        throw new ArgumentException("Either --text or --file must be specified");
                    var embedding = await embeddingService.GetEmbeddingAsync(text);
                    await vectorRepository.UpsertVectorsAsync(collection, [(embedding, text)]);
                    logger.LogInformation($"Successfully inserted {text} into {collection}");
                    Console.WriteLine($"Inserted into collection {collection}: \n Embedding: {embedding}");
                }
                else
                {
                    if (!File.Exists(filePath))
                    {
                        logger.LogError($"File {filePath} does not exist");
                        throw new FileNotFoundException($"File {filePath} does not exist");
                    }

                    await ProcessFileAsync(collection, filePath);
                }
            },
            collectionOption, textOption, filePathOption);
        insertCommand.AddOption(textOption);
        insertCommand.AddOption(collectionOption);
        insertCommand.AddOption(filePathOption);

        var promptCommand = new Command("prompt", "Prompt Command with similarity search");
        promptCommand.SetHandler(async (collection, text, limit) =>
            {
                var queryEmbedding = await embeddingService.GetEmbeddingAsync(text);
                var results = (await vectorRepository.SearchAsync(collection, queryEmbedding, limit)).ToImmutableList();
                Console.WriteLine("\nContext from similarity search:");
                Console.WriteLine("-----------------------------");
                foreach (var result in results)
                {
                    Console.WriteLine($"\nScore: {result.Score:F3}");
                    Console.WriteLine($"Text: {result.Payload}");
                }

                var response = await promptService.GetResponseAsync(text, results);
                Console.WriteLine("\nAI Response:");
                Console.WriteLine("------------");
                Console.WriteLine(response);
            },
            collectionOption, textOption, limitOption);
        promptCommand.AddOption(textOption);
        promptCommand.AddOption(collectionOption);
        promptCommand.AddOption(limitOption);


        rootCommand.AddCommand(searchCommand);
        rootCommand.AddCommand(insertCommand);
        rootCommand.AddCommand(promptCommand);
        await rootCommand.InvokeAsync(args);
    }

    private async Task ProcessFileAsync(string collection, string filePath, int batchSize = 100)
    {
        var currentBatch = new List<(float[] Vector, string Text)>();
        await foreach (var line in fileReader.ReadLinesAsync(filePath))
        {
            var embedding = await embeddingService.GetEmbeddingAsync(line);
            currentBatch.Add((embedding, line));
            if (currentBatch.Count >= batchSize)
            {
                await vectorRepository.UpsertVectorsAsync(collection, currentBatch);
                logger.LogInformation($"Successfully inserted {currentBatch.Count} vectors into {collection}");
                currentBatch.Clear();
            }
        }

        if (currentBatch.Count > 0)
        {
            await vectorRepository.UpsertVectorsAsync(collection, currentBatch);
            logger.LogInformation($"Successfully inserted final {currentBatch.Count} vectors into {collection}");
        }
    }

    private async Task EnsureCollectionExists(string collectionName)
    {
        var isHealthy = await vectorRepository.IsHealthyAsync();
        if (!isHealthy) throw new Exception("Vector database is not healthy");

        await vectorRepository.EnsureCollectionExistsAsync(collectionName);
    }
}