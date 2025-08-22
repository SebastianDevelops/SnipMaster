namespace SnipMaster.Conversion.Services;

public interface IUniversalConversionService
{
    Task<string> ConvertFileAsync(string inputFilePath, string outputFormat, string? outputFilePath = null, IProgress<int>? progress = null);
    Task<ConversionInfo> GetConversionInfoAsync(string inputFilePath, string outputFormat);
    string[] GetSupportedFormats(string inputFilePath);
    bool IsConversionSupported(string inputFilePath, string outputFormat);
}

public class ConversionInfo
{
    public ConversionServiceType ServiceType { get; set; }
    public string InputFormat { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = string.Empty;
    public bool IsSupported { get; set; }
    public string[] AvailableFormats { get; set; } = Array.Empty<string>();
}