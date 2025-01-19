namespace VectorSearch.Services;

public interface IPromptService
{
    Task<string> GetResponseAsync(string promptText, IEnumerable<SearchResult> similarityResults);
}