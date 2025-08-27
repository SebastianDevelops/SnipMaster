using System.Text.RegularExpressions;

namespace SnippetMasterWPF.Infrastructure.OCR.Extensions;

public static class LayoutPreserver
{
    public static string PreserveLayout(this string text)
    {
        return text
            .RestoreParagraphs()
            .PreserveIndentation()
            .RestoreLineBreaks();
    }
    
    public static string RestoreParagraphs(this string text)
    {
        return Regex.Replace(text, @"\n\s*\n", "\n\n");
    }
    
    public static string PreserveIndentation(this string text)
    {
        var lines = text.Split('\n');
        return string.Join("\n", lines.Select(line => 
            Regex.IsMatch(line, @"^\s{2,}") ? line : line.TrimStart()));
    }
    
    public static string RestoreLineBreaks(this string text)
    {
        return Regex.Replace(text, @"([.!?])\s+([A-Z])", "$1\n$2");
    }
    
    public static string FixRemoveArtifacts(this string text, bool isCode = false)
    {
        // Only remove actual control characters, preserve all printable symbols
        return Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
    }
}