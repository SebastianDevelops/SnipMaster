using System.Diagnostics;

namespace SnipMaster.Conversion.Services;

public class FileConversionService : IFileConversionService
{
    private readonly string[] _videoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".flv", ".m4v" };
    private readonly string[] _audioExtensions = { ".mp3", ".ogg", ".wav", ".aac", ".flac", ".m4a", ".wma" };

    public bool IsVideoFile(string filePath) => 
        _videoExtensions.Contains(Path.GetExtension(filePath).ToLower());

    public bool IsAudioFile(string filePath) => 
        _audioExtensions.Contains(Path.GetExtension(filePath).ToLower());

    public async Task<string> ConvertVideoAsync(string inputFilePath, string outputFormat, string? outputFilePath = null, IProgress<int>? progress = null)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        outputFilePath ??= Path.ChangeExtension(inputFilePath, outputFormat);
        
        var ffmpegArgs = GetVideoConversionArgs(outputFormat);
        return await ExecuteFFmpegConversionAsync(inputFilePath, outputFilePath, ffmpegArgs, progress);
    }

    public async Task<string> ConvertAudioAsync(string inputFilePath, string outputFormat, string? outputFilePath = null, IProgress<int>? progress = null)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        outputFilePath ??= Path.ChangeExtension(inputFilePath, outputFormat);
        
        var ffmpegArgs = GetAudioConversionArgs(outputFormat);
        return await ExecuteFFmpegConversionAsync(inputFilePath, outputFilePath, ffmpegArgs, progress);
    }

    public async Task<string> ExtractAudioAsync(string videoFilePath, string audioFormat, string? outputFilePath = null, IProgress<int>? progress = null)
    {
        if (!File.Exists(videoFilePath))
            throw new FileNotFoundException($"Video file not found: {videoFilePath}");

        outputFilePath ??= Path.ChangeExtension(videoFilePath, audioFormat);
        
        var ffmpegArgs = GetAudioExtractionArgs(audioFormat);
        return await ExecuteFFmpegConversionAsync(videoFilePath, outputFilePath, ffmpegArgs, progress);
    }

    private string GetVideoConversionArgs(string format)
    {
        return format.ToLower() switch
        {
            "mp4" => "-c:v libx264 -preset fast -crf 23 -c:a aac",
            "mov" => "-c:v libx264 -preset fast -crf 23 -c:a aac -f mov",
            "avi" => "-c:v libx264 -preset fast -crf 23 -c:a mp3",
            "mkv" => "-c:v libx264 -preset fast -crf 23 -c:a aac -f matroska",
            "webm" => "-c:v libvpx-vp9 -crf 30 -c:a libopus",
            _ => "-c:v libx264 -preset fast -crf 23 -c:a aac"
        };
    }

    private string GetAudioConversionArgs(string format)
    {
        return format.ToLower() switch
        {
            "mp3" => "-vn -c:a libmp3lame -b:a 192k",
            "ogg" => "-vn -c:a libvorbis -b:a 192k",
            "wav" => "-vn -c:a pcm_s16le",
            "aac" => "-vn -c:a aac -b:a 192k",
            "flac" => "-vn -c:a flac",
            _ => "-vn -c:a libmp3lame -b:a 192k"
        };
    }

    private string GetAudioExtractionArgs(string format)
    {
        return format.ToLower() switch
        {
            "mp3" => "-vn -c:a libmp3lame -b:a 192k",
            "ogg" => "-vn -c:a libvorbis -b:a 192k",
            "wav" => "-vn -c:a pcm_s16le",
            "aac" => "-vn -c:a aac -b:a 192k",
            "flac" => "-vn -c:a flac",
            _ => "-vn -c:a libmp3lame -b:a 192k"
        };
    }

    private async Task<string> ExecuteFFmpegConversionAsync(string inputPath, string outputPath, string arguments, IProgress<int>? progress)
    {
        var duration = await GetMediaDurationAsync(inputPath);
        
        if (await TryFFmpegConversionAsync(inputPath, outputPath, arguments, duration, progress, true) ||
            await TryFFmpegConversionAsync(inputPath, outputPath, arguments, duration, progress, false))
        {
            return outputPath;
        }
        
        throw new InvalidOperationException("FFmpeg conversion failed");
    }

    private async Task<bool> TryFFmpegConversionAsync(string inputPath, string outputPath, string arguments, double duration, IProgress<int>? progress, bool useBundled)
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

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var progressTask = ParseFFmpegProgressAsync(process.StandardError, duration, progress);

            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(30));
            var processTask = process.WaitForExitAsync();

            var completedTask = await Task.WhenAny(processTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                try { process.Kill(); } catch { }
                return false;
            }

            await Task.WhenAll(stdoutTask, progressTask);

            return process.ExitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<double> GetMediaDurationAsync(string inputPath)
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

                var lines = content.Split('\n');
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    ProcessProgressLine(lines[i], totalDuration, progress);
                }

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

    private string GetBundledFFmpegPath()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDirectory, "ffmpeg", "ffmpeg.exe");
    }
}