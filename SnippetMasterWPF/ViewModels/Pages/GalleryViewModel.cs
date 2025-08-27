using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SnippetMasterWPF.Services;
using RelayCommand = SnippetMasterWPF.Infrastructure.Mvvm.RelayCommand;

namespace SnippetMasterWPF.ViewModels.Pages;

public partial class GalleryViewModel : ObservableObject
{
    private readonly IScreenshotGeneratorService _screenshotService;
    
    public ObservableCollection<GalleryItem> Screenshots { get; } = new();
    
    public ICommand RefreshCommand => new RelayCommand(LoadScreenshots);
    public ICommand OpenFileCommand => new Infrastructure.Mvvm.RelayCommand<string>(OpenFile);
    public ICommand DeleteFileCommand => new Infrastructure.Mvvm.RelayCommand<string>(DeleteFile);

    public GalleryViewModel(IScreenshotGeneratorService screenshotService)
    {
        _screenshotService = screenshotService;
        LoadScreenshots();
    }

    private void LoadScreenshots()
    {
        Screenshots.Clear();
        
        var files = _screenshotService.GetGeneratedScreenshots();
        foreach (var file in files)
        {
            try
            {
                var item = new GalleryItem
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    CreatedDate = File.GetCreationTime(file),
                    Thumbnail = LoadThumbnail(file)
                };
                Screenshots.Add(item);
            }
            catch
            {
                // Skip files that can't be loaded
            }
        }
    }

    private BitmapImage LoadThumbnail(string filePath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(filePath);
        bitmap.DecodePixelWidth = 200;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void OpenFile(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
    }

    private void DeleteFile(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                LoadScreenshots();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to delete file: {ex.Message}", "Error");
            }
        }
    }
}

public class GalleryItem
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public BitmapImage? Thumbnail { get; set; }
}