namespace SnippetMaster.Api.Services;

public interface ITextFormatterService
{
    public Task<string> FormatTextSnippet(string text);
}