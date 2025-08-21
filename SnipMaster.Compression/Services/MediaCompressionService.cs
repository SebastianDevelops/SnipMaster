using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using ZstdNet;

namespace SnipMaster.Compression.Services;

public class MediaCompressionService : IMediaCompressionService
{
    private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
    private readonly string[] _videoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".m4v" };
    private readonly string[] _losslessImageExtensions = { ".png", ".bmp", ".tiff" };
    private readonly string[] _compressedImageExtensions = { ".jpg", ".jpeg", ".gif", ".webp" };

    public bool IsImageFile(string filePath) => 
        _imageExtensions.Contains(Path.GetExtension(filePath).ToLower());

    public bool IsVideoFile(string filePath) => 
        _videoExtensions.Contains(Path.GetExtension(filePath).ToLower());

    public async Task<string> CompressImageAsync(string inputFilePath, string? outputFilePath = null)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        outputFilePath ??= inputFilePath;
        var extension = Path.GetExtension(inputFilePath).ToLower();

        // For PNG, BMP, TIFF - use Zstandard compression
        if (_losslessImageExtensions.Contains(extension))
        {
            return await CompressWithZstandardAsync(inputFilePath, outputFilePath);
        }

        // For already compressed formats (JPG, GIF, WebP) - keep as-is
        if (_compressedImageExtensions.Contains(extension))
        {
            if (inputFilePath != outputFilePath)
            {
                File.Copy(inputFilePath, outputFilePath, true);
            }
            return outputFilePath;
        }

        // Fallback to Zstandard for unknown formats
        return await CompressWithZstandardAsync(inputFilePath, outputFilePath);
    }

    public async Task<string> CompressVideoAsync(string inputFilePath, string? outputFilePath = null, bool lossless = true, IProgress<int>? progress = null)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        outputFilePath ??= inputFilePath;

        if (lossless)
        {
            return await CompressVideoLosslessAsync(inputFilePath, outputFilePath, progress);
        }
        else
        {
            return await CompressVideoLossyAsync(inputFilePath, outputFilePath, progress);
        }
    }

    private async Task<string> OptimizeLosslessImageAsync(string inputPath, string outputPath)
    {
        try
        {
            using var originalImage = Image.FromFile(inputPath);
            
            // Save as optimized PNG with maximum compression
            var encoder = GetEncoder(ImageFormat.Png);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

            // Save with optimal settings
            originalImage.Save(outputPath, encoder, encoderParams);
            
            return await Task.FromResult(outputPath);
        }
        catch
        {
            return await ApplyDeflateCompressionAsync(inputPath, outputPath);
        }
    }

    private async Task<string> CompressVideoLosslessAsync(string inputPath, string outputPath, IProgress<int>? progress = null)
    {
        var tempMkvPath = Path.ChangeExtension(outputPath, ".mkv");
        
        if (await TryRepackageVideoAsync(inputPath, tempMkvPath, progress))
        {
            var originalSize = new FileInfo(inputPath).Length;
            var repackagedSize = new FileInfo(tempMkvPath).Length;
            
            if (repackagedSize < originalSize)
            {
                if (tempMkvPath != outputPath)
                {
                    File.Move(tempMkvPath, outputPath);
                }
                return outputPath;
            }
            else
            {
                File.Delete(tempMkvPath);
            }
        }

        return await TryAlternativeCompressionAsync(inputPath, outputPath);
    }

    private async Task<bool> TryRepackageVideoAsync(string inputPath, string outputPath, IProgress<int>? progress = null)
    {
        var duration = await GetVideoDurationAsync(inputPath);
        return await TryFFmpegCommandWithProgress(inputPath, outputPath, "-c copy -f matroska", duration, progress, true) ||
               await TryFFmpegCommandWithProgress(inputPath, outputPath, "-c copy -f matroska", duration, progress, false);
    }

    private async Task<bool> TrySystemFFmpegAsync(string inputPath, string outputPath)
    {
        return await TryFFmpegCommand(inputPath, outputPath, "-c copy -f matroska");
    }
    
    private async Task<string> CompressVideoLossyAsync(string inputPath, string outputPath, IProgress<int>? progress = null)
    {
        // Get video duration for progress calculation
        var duration = await GetVideoDurationAsync(inputPath);
        
        var commands = new[]
        {
            "-c:v libx264 -preset fast -crf 23 -c:a aac -movflags +faststart",
            "-c:v libx264 -preset medium -crf 23 -c:a aac",
            "-c:v libx264 -crf 23 -c:a copy"
        };
        
        foreach (var args in commands)
        {
            if (await TryFFmpegCommandWithProgress(inputPath, outputPath, args, duration, progress) ||
                await TryFFmpegCommandWithProgress(inputPath, outputPath, args, duration, progress, true))
            {
                return outputPath;
            }
        }
        
        return await TryAlternativeCompressionAsync(inputPath, outputPath);
    }
    
    private async Task<double> GetVideoDurationAsync(string inputPath)
    {
        try
        {
            var ffmpegPath = File.Exists(GetBundledFFmpegPath()) ? GetBundledFFmpegPath() : "ffmpeg";
            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{inputPath}\" -f null -",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return 0;

            var output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse duration from FFmpeg output: Duration: 00:02:30.45
            var match = System.Text.RegularExpressions.Regex.Match(output, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
            if (match.Success)
            {
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = double.Parse(match.Groups[3].Value);
                return hours * 3600 + minutes * 60 + seconds;
            }
        }
        catch { }
        return 0;
    }

    private async Task<bool> TryFFmpegCommandWithProgress(string inputPath, string outputPath, string arguments, double duration, IProgress<int>? progress, bool useBundled = false)
    {
        try
        {
            var ffmpegPath = useBundled ? GetBundledFFmpegPath() : "ffmpeg";
            if (useBundled && !File.Exists(ffmpegPath))
                return false;
                
            if (File.Exists(outputPath))
                File.Delete(outputPath);
                
            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{inputPath}\" {arguments} \"{outputPath}\" -y",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            // Read both stdout and stderr asynchronously to prevent deadlock
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var progressTask = ParseFFmpegProgressAsync(process.StandardError, duration, progress);
            
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(15));
            var processTask = process.WaitForExitAsync();
            
            var completedTask = await Task.WhenAny(processTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                try { process.Kill(); } catch { }
                return false;
            }
            
            // Wait for all stream reading to complete
            await Task.WhenAll(stdoutTask, progressTask);
            
            if (File.Exists(outputPath))
            {
                var outputSize = new FileInfo(outputPath).Length;
                return process.ExitCode == 0 && outputSize > 0;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task ParseFFmpegProgressAsync(StreamReader reader, double totalDuration, IProgress<int>? progress)
    {
        try
        {
            var buffer = new char[4096];
            var lineBuilder = new System.Text.StringBuilder();
            
            while (true)
            {
                var bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                
                lineBuilder.Append(buffer, 0, bytesRead);
                var content = lineBuilder.ToString();
                
                // Process complete lines
                var lines = content.Split('\n');
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    ProcessProgressLine(lines[i], totalDuration, progress);
                }
                
                // Keep the last incomplete line
                lineBuilder.Clear();
                if (lines.Length > 0)
                {
                    lineBuilder.Append(lines[lines.Length - 1]);
                }
            }
        }
        catch { }
    }
    
    private void ProcessProgressLine(string line, double totalDuration, IProgress<int>? progress)
    {
        try
        {
            // Parse time from FFmpeg stderr: time=00:01:23.45
            var timeMatch = System.Text.RegularExpressions.Regex.Match(line, @"time=(\d{2}):(\d{2}):(\d{2}\.\d{2})");
            if (timeMatch.Success && totalDuration > 0)
            {
                var hours = int.Parse(timeMatch.Groups[1].Value);
                var minutes = int.Parse(timeMatch.Groups[2].Value);
                var seconds = double.Parse(timeMatch.Groups[3].Value);
                var currentTime = hours * 3600 + minutes * 60 + seconds;
                
                var progressPercent = (int)Math.Min(95, (currentTime / totalDuration) * 100);
                progress?.Report(progressPercent);
            }
        }
        catch { }
    }

    private async Task<bool> TryFFmpegCommand(string inputPath, string outputPath, string arguments, bool useBundled = false)
    {
        try
        {
            var ffmpegPath = useBundled ? GetBundledFFmpegPath() : "ffmpeg";
            if (useBundled && !File.Exists(ffmpegPath))
                return false;
                
            // Clean up any existing output file
            if (File.Exists(outputPath))
                File.Delete(outputPath);
                
            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{inputPath}\" {arguments} \"{outputPath}\" -y",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            // Add timeout for video processing (15 minutes max)
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(15));
            var processTask = process.WaitForExitAsync();
            
            var completedTask = await Task.WhenAny(processTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                // Timeout occurred, kill the process
                try { process.Kill(); } catch { }
                return false;
            }
            
            // Verify the output file exists and has reasonable size
            if (File.Exists(outputPath))
            {
                var outputSize = new FileInfo(outputPath).Length;
                return process.ExitCode == 0 && outputSize > 0;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    private string GetBundledFFmpegPath()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDirectory, "ffmpeg", "ffmpeg.exe");
    }

    private async Task<string> ApplyDeflateCompressionAsync(string inputPath, string outputPath)
    {
        using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal);

        await inputStream.CopyToAsync(deflateStream);
        return outputPath;
    }

    private async Task<string> TryAlternativeCompressionAsync(string inputPath, string outputPath)
    {
        // Try Zstandard compression as alternative
        try
        {
            return await CompressWithZstandardAsync(inputPath, outputPath);
        }
        catch
        {
            // Final fallback to deflate
            return await ApplyDeflateCompressionAsync(inputPath, outputPath);
        }
    }

    private async Task<string> CompressWithZstandardAsync(string inputPath, string outputPath)
    {
        var inputBytes = await File.ReadAllBytesAsync(inputPath);
        using var compressor = new Compressor();
        var compressedBytes = compressor.Wrap(inputBytes);
        await File.WriteAllBytesAsync(outputPath, compressedBytes);
        return outputPath;
    }

    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageDecoders();
        return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid) 
               ?? throw new NotSupportedException($"No encoder found for {format}");
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