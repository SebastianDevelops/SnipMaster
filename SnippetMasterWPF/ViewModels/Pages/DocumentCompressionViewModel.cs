using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SnipMaster.Compression.Services;
using SnippetMasterWPF.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace SnippetMasterWPF.ViewModels.Pages;

public partial class DocumentCompressionViewModel : ObservableObject
{
    private readonly ICompressionService _compressionService;
    private readonly INotificationService _notificationService;
    private readonly string _compressedFilesFolder;

    [ObservableProperty]
    private ObservableCollection<CompressedFileInfo> _compressedFiles = new();

    [ObservableProperty]
    private bool _isProcessing = false;

    [ObservableProperty]
    private string _processingStatus = string.Empty;

    [ObservableProperty]
    private int _progressPercentage = 0;

    public bool IsNotProcessing => !IsProcessing;

    private readonly string[] _supportedExtensions = { ".pdf", ".docx", ".txt", ".csv", ".xml", ".rtf", ".doc" };

    public DocumentCompressionViewModel(ICompressionService compressionService, INotificationService notificationService)
    {
        _compressionService = compressionService;
        _notificationService = notificationService;
        _compressedFilesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SnipMaster", "Compressed");
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
            Filter = "Document Files|*.pdf;*.docx;*.txt;*.csv;*.xml;*.rtf;*.doc|All Files|*.*",
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
            _notificationService.ShowNotification("Invalid Files", "Only document files are supported");
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
                var compressedPath = await _compressionService.CompressFileAsync(file, outputPath, CompressionType.Zstandard);
                var compressionInfo = await _compressionService.GetCompressionInfoAsync(file, compressedPath);
                
                // Complete progress for this file
                ProgressPercentage = ((i + 1) * 100) / validFiles.Length;

                var fileInfo = new CompressedFileInfo
                {
                    FileName = Path.GetFileName(file),
                    FilePath = compressedPath,
                    OriginalSize = compressionInfo.OriginalSize,
                    CompressedSize = compressionInfo.CompressedSize,
                    SpaceSavedPercentage = compressionInfo.SpaceSavedPercentage,
                    CompressedDate = DateTime.Now
                };

                CompressedFiles.Insert(0, fileInfo);
                _notificationService.ShowNotification("Compressed", $"{fileInfo.FileName} compressed successfully");
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
    private void DeleteFile(CompressedFileInfo fileInfo)
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
            
            CompressedFiles.Add(new CompressedFileInfo
            {
                FileName = Path.GetFileName(file),
                FilePath = file,
                CompressedSize = fileInfo.Length,
                CompressedDate = fileInfo.CreationTime
            });
        }
    }
}

public class CompressedFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public double SpaceSavedPercentage { get; set; }
    public DateTime CompressedDate { get; set; }
}