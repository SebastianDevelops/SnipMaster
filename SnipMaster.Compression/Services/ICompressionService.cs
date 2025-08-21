namespace SnipMaster.Compression.Services;

public interface ICompressionService
{
    Task<string> CompressFileAsync(string inputFilePath, string? outputFilePath = null, CompressionType type = CompressionType.GZip);
    Task<string> DecompressFileAsync(string compressedFilePath, string? outputFilePath = null, CompressionType type = CompressionType.GZip);
    Task<CompressionResult> GetCompressionInfoAsync(string originalFilePath, string compressedFilePath);
}

public enum CompressionType
{
    GZip,
    Deflate
}

public class CompressionResult
{
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize : 0;
    public double SpaceSavedPercentage => (1 - CompressionRatio) * 100;
}