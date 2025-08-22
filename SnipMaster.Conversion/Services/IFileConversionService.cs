namespace SnipMaster.Conversion.Services;

public interface IFileConversionService
{
    Task<string> ConvertVideoAsync(string inputFilePath, string outputFormat, string? outputFilePath = null, IProgress<int>? progress = null);
    Task<string> ConvertAudioAsync(string inputFilePath, string outputFormat, string? outputFilePath = null, IProgress<int>? progress = null);
    Task<string> ExtractAudioAsync(string videoFilePath, string audioFormat, string? outputFilePath = null, IProgress<int>? progress = null);
    bool IsVideoFile(string filePath);
    bool IsAudioFile(string filePath);
}

public static class ConversionFormats
{
    // Video formats
    public const string MP4 = "mp4";
    public const string MOV = "mov";
    public const string AVI = "avi";
    public const string MKV = "mkv";
    public const string WEBM = "webm";
    
    // Audio formats
    public const string MP3 = "mp3";
    public const string OGG = "ogg";
    public const string WAV = "wav";
    public const string AAC = "aac";
    public const string FLAC = "flac";
}