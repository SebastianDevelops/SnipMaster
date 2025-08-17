namespace SnippetMasterWPF.Services;

public interface IApiClient
{
    Task<string> ProcessSnippetAsync(string text);
}