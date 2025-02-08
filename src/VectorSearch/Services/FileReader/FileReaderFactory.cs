namespace VectorSearch.Services.FileReader;

public class FileReaderFactory(IDictionary<string, IFileReader> readers) : IFileReader
{
    public IAsyncEnumerable<string> ReadLinesAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        if (!readers.TryGetValue(extension, out var reader))
            throw new NotSupportedException($"File type {extension} is not supported");

        return reader.ReadLinesAsync(filePath);
    }
}