using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using SnipMaster.Conversion.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.ComponentModel;

namespace SnippetMasterWPF.ViewModels.Pages
{
    public partial class FileConversionViewModel : ObservableObject
    {
        private readonly IConversionServiceFactory _conversionServiceFactory;

        [ObservableProperty]
        private ObservableCollection<ConversionItemViewModel> _conversionItems = new();

        [ObservableProperty]
        private ObservableCollection<string> _imageFormats = new() { "png", "jpg", "jpeg", "webp", "bmp", "gif", "tiff", "pdf" };

        [ObservableProperty]
        private ObservableCollection<string> _documentFormats = new() { "pdf", "docx", "txt", "epub", "png", "jpg", "jpeg" };

        [ObservableProperty]
        private ObservableCollection<string> _videoAudioFormats = new() { "mp4", "avi", "mov", "mp3", "wav", "flac" };

        [ObservableProperty]
        private string _selectedImageFormat = "png";

        [ObservableProperty]
        private string _selectedDocumentFormat = "pdf";

        [ObservableProperty]
        private string _selectedVideoAudioFormat = "mp4";

        [ObservableProperty]
        private bool _hasItems;

        [ObservableProperty]
        private ObservableCollection<HistoryItemViewModel> _historyItems = new();

        [ObservableProperty]
        private ObservableCollection<HistoryItemViewModel> _filteredHistoryItems = new();

        [ObservableProperty]
        private string _historySearchText = string.Empty;

        [ObservableProperty]
        private bool _hasHistoryItems;

        public FileConversionViewModel(IConversionServiceFactory conversionServiceFactory)
        {
            _conversionServiceFactory = conversionServiceFactory;
            ConversionItems.CollectionChanged += (s, e) => HasItems = ConversionItems.Any();
            HistoryItems.CollectionChanged += (s, e) => 
            {
                HasHistoryItems = HistoryItems.Any();
                FilterHistory();
            };
            LoadHistory();
        }

        partial void OnHistorySearchTextChanged(string value)
        {
            FilterHistory();
        }

        [RelayCommand]
        private void SelectImageFiles()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.webp;*.svg)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.webp;*.svg",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    AddConversionItem(fileName, SelectedImageFormat, ConversionType.Image);
                }
            }
        }

        [RelayCommand]
        private void SelectDocumentFiles()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Document files (*.pdf;*.docx;*.doc;*.txt;*.epub)|*.pdf;*.docx;*.doc;*.txt;*.epub",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    AddConversionItem(fileName, SelectedDocumentFormat, ConversionType.Document);
                }
            }
        }

        [RelayCommand]
        private void SelectMediaFiles()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Media files (*.mp4;*.avi;*.mov;*.mp3;*.wav;*.flac)|*.mp4;*.avi;*.mov;*.mp3;*.wav;*.flac",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    AddConversionItem(fileName, SelectedVideoAudioFormat, ConversionType.Media);
                }
            }
        }

        [RelayCommand]
        private void RemoveItem(ConversionItemViewModel item)
        {
            ConversionItems.Remove(item);
        }

        [RelayCommand]
        private void ClearAll()
        {
            ConversionItems.Clear();
        }

        [RelayCommand]
        private async Task ConvertAll()
        {
            var tasks = ConversionItems.Where(item => item.Status == "Ready")
                                     .Select(ConvertItem);
            
            await Task.WhenAll(tasks);
        }

        private void AddConversionItem(string filePath, string outputFormat, ConversionType type)
        {
            var item = new ConversionItemViewModel
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                OutputFormat = outputFormat,
                ConversionType = type,
                Status = "Ready"
            };

            // Check if this is PDF to image conversion
            if (type == ConversionType.Document && Path.GetExtension(filePath).ToLower() == ".pdf" && 
                (outputFormat == "png" || outputFormat == "jpg" || outputFormat == "jpeg"))
            {
                item.IsPdfToImage = true;
                try
                {
                    var documentService = _conversionServiceFactory.GetDocumentConversionService(filePath);
                    item.TotalPages = documentService.GetPdfPageCount(filePath);
                }
                catch (UnauthorizedAccessException)
                {
                    item.RequiresPassword = true;
                    item.Status = "Password Required";
                }
                catch
                {
                    item.TotalPages = 1;
                }
            }

            ConversionItems.Add(item);
        }

        private async Task ConvertItem(ConversionItemViewModel item)
        {
            try
            {
                item.Status = "Converting...";
                item.IsConverting = true;

                var progress = new Progress<int>(value => item.Progress = value);
                
                // Save to Documents/SnipMaster/Converted directory
                var convertedDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SnipMaster", "Converted");
                Directory.CreateDirectory(convertedDir);
                var fileName = Path.GetFileNameWithoutExtension(item.FilePath);
                var outputPath = Path.Combine(convertedDir, $"{fileName}.{item.OutputFormat}");

                switch (item.ConversionType)
                {
                    case ConversionType.Image:
                        var imageService = _conversionServiceFactory.GetImageConversionService(item.FilePath);
                        await imageService.ConvertImageAsync(item.FilePath, item.OutputFormat, outputPath, progress);
                        break;

                    case ConversionType.Document:
                        var documentService = _conversionServiceFactory.GetDocumentConversionService(item.FilePath);
                        if (item.IsPdfToImage)
                        {
                            await documentService.ConvertPdfToImageAsync(item.FilePath, item.OutputFormat, outputPath, item.SelectedPage - 1, string.IsNullOrEmpty(item.PdfPassword) ? null : item.PdfPassword, progress);
                        }
                        else
                        {
                            await documentService.ConvertDocumentAsync(item.FilePath, item.OutputFormat, outputPath, progress);
                        }
                        break;

                    case ConversionType.Media:
                        var fileService = _conversionServiceFactory.GetConversionService(item.FilePath);
                        if (fileService.IsVideoFile(item.FilePath))
                        {
                            await fileService.ConvertVideoAsync(item.FilePath, item.OutputFormat, outputPath, progress);
                        }
                        else if (fileService.IsAudioFile(item.FilePath))
                        {
                            await fileService.ConvertAudioAsync(item.FilePath, item.OutputFormat, outputPath, progress);
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported media file type: {Path.GetExtension(item.FilePath)}");
                        }
                        break;
                }

                item.Status = "Completed";
                item.OutputPath = outputPath;
                
                // Add to history
                AddToHistory(outputPath);
            }
            catch (Exception ex)
            {
                item.Status = $"Error: {ex.Message}";
            }
            finally
            {
                item.IsConverting = false;
                item.Progress = 0;
            }
        }

        [RelayCommand]
        private void OpenFolder(HistoryItemViewModel historyItem)
        {
            if (File.Exists(historyItem.FilePath))
            {
                Process.Start("explorer.exe", $"/select,\"{historyItem.FilePath}\"");
            }
        }

        private void AddToHistory(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var fileInfo = new FileInfo(filePath);
            var historyItem = new HistoryItemViewModel
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                ConvertedDate = DateTime.Now.ToString("MMM dd, yyyy HH:mm"),
                FileSize = FormatFileSize(fileInfo.Length),
                FileIcon = GetFileIcon(fileInfo.Extension)
            };

            HistoryItems.Insert(0, historyItem);
            SaveHistory();
        }

        private void FilterHistory()
        {
            FilteredHistoryItems.Clear();
            var filtered = string.IsNullOrWhiteSpace(HistorySearchText) 
                ? HistoryItems 
                : HistoryItems.Where(h => h.FileName.Contains(HistorySearchText, StringComparison.OrdinalIgnoreCase));
            
            foreach (var item in filtered)
            {
                FilteredHistoryItems.Add(item);
            }
        }

        private void LoadHistory()
        {
            // Load from converted files directory
            var convertedDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SnipMaster", "Converted");
            if (Directory.Exists(convertedDir))
            {
                var files = Directory.GetFiles(convertedDir, "*", SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).StartsWith("."))
                    .OrderByDescending(f => new FileInfo(f).CreationTime);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    HistoryItems.Add(new HistoryItemViewModel
                    {
                        FilePath = file,
                        FileName = fileInfo.Name,
                        ConvertedDate = fileInfo.CreationTime.ToString("MMM dd, yyyy HH:mm"),
                        FileSize = FormatFileSize(fileInfo.Length),
                        FileIcon = GetFileIcon(fileInfo.Extension)
                    });
                }
            }
        }

        private void SaveHistory()
        {
            // History is automatically saved by file system
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private static string GetFileIcon(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "DocumentPdf24",
                ".docx" or ".doc" => "Document24",
                ".txt" => "DocumentText24",
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp" => "Image24",
                ".mp4" or ".avi" or ".mov" => "Video24",
                ".mp3" or ".wav" or ".flac" => "MusicNote124",
                _ => "Document24"
            };
        }
    }

    public partial class ConversionItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _outputFormat = string.Empty;

        [ObservableProperty]
        private string _outputPath = string.Empty;

        [ObservableProperty]
        private ConversionType _conversionType;

        [ObservableProperty]
        private string _status = "Ready";

        [ObservableProperty]
        private int _progress;

        [ObservableProperty]
        private bool _isConverting;

        [ObservableProperty]
        private int _selectedPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private string _pdfPassword = string.Empty;

        [ObservableProperty]
        private bool _requiresPassword;

        [ObservableProperty]
        private bool _isPdfToImage;
    }

    public partial class HistoryItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _convertedDate = string.Empty;

        [ObservableProperty]
        private string _fileSize = string.Empty;

        [ObservableProperty]
        private string _fileIcon = "Document24";
    }

    public enum ConversionType
    {
        Image,
        Document,
        Media
    }
}