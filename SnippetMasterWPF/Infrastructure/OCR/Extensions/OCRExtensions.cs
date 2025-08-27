using System.Text.RegularExpressions;
using SnippetMasterWPF.Services;

namespace SnippetMasterWPF.Infrastructure.OCR.Extensions;

public static class OcrExtensions
{
    public static string FixCommonOcrErrors(this string text, float confidence = 1.0f)
    {
        // Only apply corrections if OCR confidence is low
        if (confidence > 0.8f) return text;
        
        // Context-aware corrections for low confidence scenarios
        text = Regex.Replace(text, @"\brn\b", "m", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bvv\b", "w", RegexOptions.IgnoreCase);
        
        return text;
    }

    public static string CorrectSpelling(this string text)
    {
        var spellCorrections = new Dictionary<string, string>
        {
            { "teh", "the" }, { "adn", "and" }, { "taht", "that" },
            { "wihch", "which" }, { "recieve", "receive" }
        };
        
        foreach (var correction in spellCorrections)
            text = Regex.Replace(text, $@"\b{correction.Key}\b", correction.Value, RegexOptions.IgnoreCase);
        
        return text;
    }

    public static string NormalizeWhitespace(this string text)
    {
        // Preserve line breaks but normalize spaces within lines
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var normalizedLines = lines.Select(line => Regex.Replace(line, @"[ \t]+", " ").Trim());
        return string.Join(Environment.NewLine, normalizedLines).Trim();
    }

    public static string RemoveArtifacts(this string text, Enums.DocumentType docType)
    {
        if (docType == Enums.DocumentType.Code)
            return text;
            
        // Only remove truly problematic characters, preserve important punctuation
        return Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
    }
}