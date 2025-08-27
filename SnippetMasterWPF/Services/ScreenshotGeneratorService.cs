using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SnippetMasterWPF.Models.Editor;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Path = System.IO.Path;

namespace SnippetMasterWPF.Services;

public class ScreenshotGeneratorService : IScreenshotGeneratorService
{
    private readonly string _outputDirectory;
    
    public ScreenshotGeneratorService()
    {
        _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SnipMaster", "Generated");
        Directory.CreateDirectory(_outputDirectory);
    }

    public async Task<string> GenerateScreenshotAsync(string code, EditorLanguage language, string? title = null)
    {
        var fileName = $"snippet_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(_outputDirectory, fileName);
        
        await CreateWebView2Screenshot(code, language, title ?? "Code Snippet", filePath);
        
        return filePath;
    }

    public List<string> GetGeneratedScreenshots()
    {
        if (!Directory.Exists(_outputDirectory))
            return new List<string>();
            
        return Directory.GetFiles(_outputDirectory, "*.png")
            .OrderByDescending(f => File.GetCreationTime(f))
            .ToList();
    }

    private async Task CreateWebView2Screenshot(string code, EditorLanguage language, string title, string outputPath)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var visual = CreateCodeVisual(code, language, title);
            SaveVisualAsPng(visual, outputPath, 800, 600);
        });
    }
    
    private Visual CreateCodeVisual(string code, EditorLanguage language, string title)
    {
        var container = new Border
        {
            Width = 800,
            Height = 600,
            Background = new LinearGradientBrush(
                System.Windows.Media.Color.FromRgb(102, 126, 234),
                System.Windows.Media.Color.FromRgb(118, 75, 162),
                45),
            Padding = new Thickness(20)
        };
        
        var codeContainer = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
            CornerRadius = new CornerRadius(12),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 20,
                ShadowDepth = 10,
                Opacity = 0.3
            }
        };
        
        var stackPanel = new StackPanel();
        
        // Title bar
        var titleBar = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
            Padding = new Thickness(20, 12, 20, 12)
        };
        
        var titleContent = new StackPanel { Orientation = Orientation.Horizontal };
        
        // Window controls
        var controls = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 15, 0) };
        controls.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 95, 87)), Margin = new Thickness(0, 0, 8, 0) });
        controls.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 189, 46)), Margin = new Thickness(0, 0, 8, 0) });
        controls.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 202, 66)) });
        
        var titleText = new TextBlock
        {
            Text = title,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204)),
            FontSize = 13
        };
        
        titleContent.Children.Add(controls);
        titleContent.Children.Add(titleText);
        titleBar.Child = titleContent;
        
        // Code area
        var codeArea = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
            Padding = new Thickness(20),
            Height = 400
        };
        
        var codeText = new TextBlock
        {
            Text = code,
            Foreground = GetLanguageColor(language),
            FontFamily = new FontFamily("Consolas, Monaco, monospace"),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        };
        
        codeArea.Child = codeText;
        
        stackPanel.Children.Add(titleBar);
        stackPanel.Children.Add(codeArea);
        codeContainer.Child = stackPanel;
        container.Child = codeContainer;
        
        return container;
    }
    
    private SolidColorBrush GetLanguageColor(EditorLanguage language)
    {
        return language switch
        {
            EditorLanguage.Csharp => new SolidColorBrush(System.Windows.Media.Color.FromRgb(86, 156, 214)),
            EditorLanguage.JavaScript => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 206, 84)),
            EditorLanguage.Python => new SolidColorBrush(System.Windows.Media.Color.FromRgb(78, 201, 176)),
            EditorLanguage.Html => new SolidColorBrush(System.Windows.Media.Color.FromRgb(227, 98, 9)),
            _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212))
        };
    }
    
    private void SaveVisualAsPng(Visual visual, string filePath, int width, int height)
    {
        if (visual is FrameworkElement element)
        {
            element.Measure(new System.Windows.Size(width, height));
            element.Arrange(new Rect(0, 0, width, height));
        }
        
        var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(visual);
        
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
        
        using var fileStream = new FileStream(filePath, FileMode.Create);
        encoder.Save(fileStream);
    }
    

    
    private string GetMonacoLanguageId(EditorLanguage language)
    {
        return language switch
        {
            EditorLanguage.Csharp => "csharp",
            EditorLanguage.JavaScript => "javascript",
            EditorLanguage.TypeScript => "typescript",
            EditorLanguage.Python => "python",
            EditorLanguage.Java => "java",
            EditorLanguage.Cpp => "cpp",
            EditorLanguage.Html => "html",
            EditorLanguage.Css => "css",
            EditorLanguage.Xml => "xml",
            EditorLanguage.Sql => "sql",
            EditorLanguage.Markdown => "markdown",
            _ => "plaintext"
        };
    }
}