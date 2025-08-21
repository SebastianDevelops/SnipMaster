using System.IO.Compression;
using ZstdNet;

namespace SnipMaster.Compression.Services;

public class CompressionService : ICompressionService
{
    public async Task<string> CompressFileAsync(string inputFilePath, string? outputFilePath = null, CompressionType type = CompressionType.Zstandard)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        outputFilePath ??= inputFilePath;

        if (type == CompressionType.Zstandard)
        {
            return await CompressWithZstandardAsync(inputFilePath, outputFilePath);
        }
        else if (type == CompressionType.GZip)
        {
            using var inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
            using var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
            using var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal);
            await inputStream.CopyToAsync(compressionStream);
        }
        else
        {
            using var inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
            using var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
            using var compressionStream = new DeflateStream(outputStream, CompressionLevel.Optimal);
            await inputStream.CopyToAsync(compressionStream);
        }

        return outputFilePath;
    }

    public async Task<string> DecompressFileAsync(string compressedFilePath, string? outputFilePath = null, CompressionType type = CompressionType.Zstandard)
    {
        if (!File.Exists(compressedFilePath))
            throw new FileNotFoundException($"Compressed file not found: {compressedFilePath}");

        outputFilePath ??= compressedFilePath + ".decompressed";

        if (type == CompressionType.Zstandard)
        {
            return await DecompressWithZstandardAsync(compressedFilePath, outputFilePath);
        }
        else if (type == CompressionType.GZip)
        {
            using var inputStream = new FileStream(compressedFilePath, FileMode.Open, FileAccess.Read);
            using var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
            using var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
            await decompressionStream.CopyToAsync(outputStream);
        }
        else
        {
            using var inputStream = new FileStream(compressedFilePath, FileMode.Open, FileAccess.Read);
            using var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
            using var decompressionStream = new DeflateStream(inputStream, CompressionMode.Decompress);
            await decompressionStream.CopyToAsync(outputStream);
        }

        return outputFilePath;
    }

    private async Task<string> CompressWithZstandardAsync(string inputPath, string outputPath)
    {
        var inputBytes = await File.ReadAllBytesAsync(inputPath);
        using var compressor = new Compressor();
        var compressedBytes = compressor.Wrap(inputBytes);
        await File.WriteAllBytesAsync(outputPath, compressedBytes);
        return outputPath;
    }

    private async Task<string> DecompressWithZstandardAsync(string inputPath, string outputPath)
    {
        var compressedBytes = await File.ReadAllBytesAsync(inputPath);
        using var decompressor = new Decompressor();
        var decompressedBytes = decompressor.Unwrap(compressedBytes);
        await File.WriteAllBytesAsync(outputPath, decompressedBytes);
        return outputPath;
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