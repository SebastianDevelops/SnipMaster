using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SnipMaster.Compression.Services;
using SnippetMasterWPF.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace SnippetMasterWPF.ViewModels.Pages;

public partial class MediaCompressionViewModel : ObservableObject
{
    private readonly IMediaCompressionService _mediaCompressionService;
    private readonly INotificationService _notificationService;
    private readonly string _compressedFilesFolder;

    [ObservableProperty]
    private ObservableCollection<CompressedMediaFileInfo> _compressedFiles = new();

    [ObservableProperty]
    private bool _isProcessing = false;

    [ObservableProperty]
    private string _processingStatus = string.Empty;

    [ObservableProperty]
    private int _progressPercentage = 0;

    [ObservableProperty]
    private bool _isLosslessMode = true;

    public bool IsNotProcessing => !IsProcessing;
    public bool IsLossyMode
    {
        get => !IsLosslessMode;
        set => IsLosslessMode = !value;
    }

    private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".svg", ".webp" };
    private readonly string[] _videoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".m4v" };
    private readonly string[] _supportedExtensions;

    public MediaCompressionViewModel(IMediaCompressionService mediaCompressionService, INotificationService notificationService)
    {
        _mediaCompressionService = mediaCompressionService;
        _notificationService = notificationService;
        _supportedExtensions = _imageExtensions.Concat(_videoExtensions).ToArray();
        _compressedFilesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SnipMaster", "CompressedMedia");
        Directory.CreateDirectory(_compressedFilesFolder);
        LoadCompressedFiles();
    }

    partial void OnIsProcessingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotProcessing));
    }

    [RelayCommand]
    private void BrowseFiles()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Media Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.tiff;*.svg;*.webp;*.mp4;*.avi;*.mov;*.wmv;*.flv;*.mkv;*.webm;*.m4v|All Files|*.*",
            Multiselect = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            ProcessDroppedFiles(openFileDialog.FileNames);
        }
    }

    public async void ProcessDroppedFiles(string[] files)
    {
        var validFiles = files.Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower())).ToArray();
        
        if (!validFiles.Any())
        {
            _notificationService.ShowNotification("Invalid Files", "Only image and video files are supported");
            return;
        }

        IsProcessing = true;
        
        for (int i = 0; i < validFiles.Length; i++)
        {
            var file = validFiles[i];
            try
            {
                var baseProgress = (i * 100) / validFiles.Length;
                ProgressPercentage = baseProgress;
                ProcessingStatus = $"Compressing {Path.GetFileName(file)} ({i + 1}/{validFiles.Length})...";
                
                var outputPath = Path.Combine(_compressedFilesFolder, Path.GetFileName(file));
                string compressedPath;
                
                if (_mediaCompressionService.IsImageFile(file))
                {
                    // Update progress for image processing
                    ProgressPercentage = baseProgress + (50 / validFiles.Length); // Mid-point progress
                    compressedPath = await _mediaCompressionService.CompressImageAsync(file, outputPath);
                }
                else if (_mediaCompressionService.IsVideoFile(file))
                {
                    // Create progress reporter for real FFmpeg progress
                    var videoProgress = new Progress<int>(percent => 
                    {
                        var fileProgress = baseProgress + (percent * 100 / validFiles.Length / 100);
                        ProgressPercentage = Math.Min(fileProgress, ((i + 1) * 100) / validFiles.Length - 5);
                    });
                    
                    // Fallback progress animation if FFmpeg progress fails
                    var fallbackTask = Task.Run(async () =>
                    {
                        await Task.Delay(5000); // Wait 5 seconds
                        if (ProgressPercentage <= baseProgress + 5) // If no progress detected
                        {
                            for (int p = 10; p <= 90; p += 10)
                            {
                                if (!IsProcessing) break;
                                var fallbackProgress = baseProgress + (p * 100 / validFiles.Length / 100);
                                ProgressPercentage = Math.Min(fallbackProgress, ((i + 1) * 100) / validFiles.Length - 10);
                                await Task.Delay(10000); // Update every 10 seconds
                            }
                        }
                    });
                    
                    compressedPath = await _mediaCompressionService.CompressVideoAsync(file, outputPath, IsLosslessMode, videoProgress);
                }
                else
                {
                    throw new NotSupportedException($"File type not supported: {Path.GetExtension(file)}");
                }
                
                var compressionInfo = await _mediaCompressionService.GetCompressionInfoAsync(file, compressedPath);
                
                // Complete progress for this file
                ProgressPercentage = ((i + 1) * 100) / validFiles.Length;

                // Check if compression was effective (at least 1% reduction)
                if (compressionInfo.SpaceSavedPercentage < 1.0)
                {
                    // Delete ineffective compressed file and copy original instead
                    if (File.Exists(compressedPath))
                        File.Delete(compressedPath);
                    
                    File.Copy(file, outputPath, true);
                    
                    var fileInfo = new CompressedMediaFileInfo
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = outputPath,
                        OriginalSize = compressionInfo.OriginalSize,
                        CompressedSize = compressionInfo.OriginalSize,
                        SpaceSavedPercentage = 0,
                        CompressedDate = DateTime.Now,
                        IsVideo = _videoExtensions.Contains(Path.GetExtension(file).ToLower())
                    };

                    CompressedFiles.Insert(0, fileInfo);
                    _notificationService.ShowNotification("Processed", $"{fileInfo.FileName} - already optimized (no compression needed)");
                }
                else
                {
                    var fileInfo = new CompressedMediaFileInfo
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = compressedPath,
                        OriginalSize = compressionInfo.OriginalSize,
                        CompressedSize = compressionInfo.CompressedSize,
                        SpaceSavedPercentage = compressionInfo.SpaceSavedPercentage,
                        CompressedDate = DateTime.Now,
                        IsVideo = _videoExtensions.Contains(Path.GetExtension(file).ToLower())
                    };

                    CompressedFiles.Insert(0, fileInfo);
                    _notificationService.ShowNotification("Compressed", $"{fileInfo.FileName} compressed successfully ({compressionInfo.SpaceSavedPercentage:F1}% saved)");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowNotification("Error", $"Failed to compress {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        ProgressPercentage = 100;
        await Task.Delay(500); // Show 100% briefly
        
        IsProcessing = false;
        ProcessingStatus = string.Empty;
        ProgressPercentage = 0;
    }

    [RelayCommand]
    private void OpenFolder(string filePath)
    {
        if (File.Exists(filePath))
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
    }

    [RelayCommand]
    private void DeleteFile(CompressedMediaFileInfo fileInfo)
    {
        try
        {
            if (File.Exists(fileInfo.FilePath))
            {
                File.Delete(fileInfo.FilePath);
                CompressedFiles.Remove(fileInfo);
                _notificationService.ShowNotification("Deleted", $"{fileInfo.FileName} deleted successfully");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowNotification("Error", $"Cannot delete file: {ex.Message}");
        }
    }

    private void LoadCompressedFiles()
    {
        if (!Directory.Exists(_compressedFilesFolder))
            return;

        var files = Directory.GetFiles(_compressedFilesFolder)
            .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
            .OrderByDescending(f => new FileInfo(f).CreationTime);

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            
            CompressedFiles.Add(new CompressedMediaFileInfo
            {
                FileName = Path.GetFileName(file),
                FilePath = file,
                CompressedSize = fileInfo.Length,
                CompressedDate = fileInfo.CreationTime,
                IsVideo = _videoExtensions.Contains(Path.GetExtension(file).ToLower())
            });
        }
    }
    

}

public class CompressedMediaFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public double SpaceSavedPercentage { get; set; }
    public DateTime CompressedDate { get; set; }
    public bool IsVideo { get; set; }
}