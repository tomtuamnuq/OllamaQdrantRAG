using System.Text;

namespace VectorSearch.Services.FileReader;

public class TxtFileReader : IFileReader
{
    public async IAsyncEnumerable<string> ReadLinesAsync(string filePath)
    {
        if (!filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only .txt files are supported");

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(fileStream, Encoding.UTF8);

        while (await reader.ReadLineAsync() is { } line)
            if (!string.IsNullOrWhiteSpace(line))
                yield return line.Trim();
    }
}