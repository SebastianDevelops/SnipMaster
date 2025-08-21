namespace SnipMaster.Compression.Services;

public interface IMediaCompressionService
{
    Task<string> CompressImageAsync(string inputFilePath, string? outputFilePath = null);
    Task<string> CompressVideoAsync(string inputFilePath, string? outputFilePath = null, bool lossless = true, IProgress<int>? progress = null);
    Task<CompressionResult> GetCompressionInfoAsync(string originalFilePath, string compressedFilePath);
    bool IsImageFile(string filePath);
    bool IsVideoFile(string filePath);
}