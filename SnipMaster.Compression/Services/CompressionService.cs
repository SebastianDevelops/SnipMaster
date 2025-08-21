using System.IO.Compression;

namespace SnipMaster.Compression.Services;

public class CompressionService : ICompressionService
{
    public async Task<string> CompressFileAsync(string inputFilePath, string? outputFilePath = null, CompressionType type = CompressionType.GZip)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        if (outputFilePath == null)
        {
            var extension = type == CompressionType.GZip ? ".gz" : string.Empty;
            outputFilePath = inputFilePath + extension;
        }

        using var inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
        using var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
        
        if (type == CompressionType.GZip)
        {
            using var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal);
            await inputStream.CopyToAsync(compressionStream);
        }
        else
        {
            using var compressionStream = new DeflateStream(outputStream, CompressionLevel.Optimal);
            await inputStream.CopyToAsync(compressionStream);
        }

        return outputFilePath;
    }

    public async Task<string> DecompressFileAsync(string compressedFilePath, string? outputFilePath = null, CompressionType type = CompressionType.GZip)
    {
        if (!File.Exists(compressedFilePath))
            throw new FileNotFoundException($"Compressed file not found: {compressedFilePath}");

        if (outputFilePath == null)
        {
            outputFilePath = type == CompressionType.GZip && compressedFilePath.EndsWith(".gz")
                ? compressedFilePath[..^3]
                : compressedFilePath + ".decompressed";
        }

        using var inputStream = new FileStream(compressedFilePath, FileMode.Open, FileAccess.Read);
        using var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
        
        if (type == CompressionType.GZip)
        {
            using var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
            await decompressionStream.CopyToAsync(outputStream);
        }
        else
        {
            using var decompressionStream = new DeflateStream(inputStream, CompressionMode.Decompress);
            await decompressionStream.CopyToAsync(outputStream);
        }

        return outputFilePath;
    }

    public async Task<CompressionResult> GetCompressionInfoAsync(string originalFilePath, string compressedFilePath)
    {
        if (!File.Exists(originalFilePath))
            throw new FileNotFoundException($"Original file not found: {originalFilePath}");
        
        if (!File.Exists(compressedFilePath))
            throw new FileNotFoundException($"Compressed file not found: {compressedFilePath}");

        var originalInfo = new FileInfo(originalFilePath);
        var compressedInfo = new FileInfo(compressedFilePath);

        return await Task.FromResult(new CompressionResult
        {
            OriginalSize = originalInfo.Length,
            CompressedSize = compressedInfo.Length
        });
    }
}