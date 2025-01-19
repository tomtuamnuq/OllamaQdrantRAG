namespace VectorSearch.Services.FileReader;

public interface IFileReader
{
    IAsyncEnumerable<string> ReadLinesAsync(string filePath);
}