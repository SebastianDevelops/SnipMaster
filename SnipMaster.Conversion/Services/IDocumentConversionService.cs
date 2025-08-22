namespace SnipMaster.Conversion.Services;

public interface IDocumentConversionService
{
    Task<string> ConvertDocumentAsync(string inputFilePath, string outputFormat, string? outputFilePath = null, IProgress<int>? progress = null);
    Task<string> ConvertPdfToImageAsync(string inputFilePath, string outputFormat, string outputFilePath, int pageNumber, string? password = null, IProgress<int>? progress = null);
    int GetPdfPageCount(string pdfFilePath, string? password = null);
    bool IsDocumentFile(string filePath);
    string[] GetSupportedFormats();
}

public static class DocumentFormats
{
    public const string PDF = "pdf";
    public const string DOCX = "docx";
    public const string DOC = "doc";
    public const string EPUB = "epub";
    public const string TXT = "txt";
    public const string RTF = "rtf";
    public const string JPG = "jpg";
    public const string PNG = "png";
}