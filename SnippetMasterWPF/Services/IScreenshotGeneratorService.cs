using SnippetMasterWPF.Models.Editor;

namespace SnippetMasterWPF.Services;

public interface IScreenshotGeneratorService
{
    Task<string> GenerateScreenshotAsync(string code, EditorLanguage language, string? title = null);
    List<string> GetGeneratedScreenshots();
}