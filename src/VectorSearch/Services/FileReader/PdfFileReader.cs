using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using LangChain.DocumentLoaders;
using LangChain.Extensions;
using LangChain.Splitters.Text;
namespace VectorSearch.Services.FileReader;

public class PdfFileReader : IFileReader
{
    private readonly RecursiveCharacterTextSplitter _splitter = new(
        chunkSize: 1000,
        chunkOverlap: 200,
        separators: ["\n\n", "\n", " ", ""]
    );

    public async IAsyncEnumerable<string> ReadLinesAsync(string filePath)
    {
        if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only .pdf files are supported");

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File {filePath} does not exist");

        var fullText = ExtractTextFromPdf(filePath);
        var chunks = _splitter.SplitText(fullText);
        foreach (var chunk in chunks)
        {
            Console.WriteLine($"Chunk: {chunk} \n ####");
            if (string.IsNullOrWhiteSpace(chunk)) continue;
            yield return chunk;
        }
    }
    private static string ExtractTextFromPdf(string filePath)
    {
        using var pdfDoc = new PdfDocument(new PdfReader(filePath, new ReaderProperties()));
        var strategy = new LocationTextExtractionStrategy();
        var text = new System.Text.StringBuilder();

        for (var i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            text.AppendLine(PdfTextExtractor.GetTextFromPage(page, strategy));
        }

        return text.ToString();
    }
}