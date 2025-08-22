using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Tiff;
using Svg;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SnipMaster.Conversion.Services;

public class ImageConversionService : IImageConversionService
{
    private readonly string[] _supportedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tiff", ".svg", ".heic", ".jfif" };

    public bool IsImageFile(string filePath) => 
        _supportedExtensions.Contains(Path.GetExtension(filePath).ToLower());

    public string[] GetSupportedFormats() => 
        new[] { "png", "jpg", "jpeg", "webp", "bmp", "gif", "tiff", "svg", "pdf" };

    public async Task<string> ConvertImageAsync(string inputFilePath, string outputFormat, string? outputFilePath = null, IProgress<int>? progress = null)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        outputFilePath ??= Path.ChangeExtension(inputFilePath, outputFormat);
        var inputExtension = Path.GetExtension(inputFilePath).ToLower();
        var outputFormatLower = outputFormat.ToLower();

        progress?.Report(10);

        try
        {
            // Handle SVG conversions separately
            if (inputExtension == ".svg")
            {
                return await ConvertFromSvgAsync(inputFilePath, outputFilePath, outputFormatLower, progress);
            }
            
            if (outputFormatLower == "svg")
            {
                throw new NotSupportedException("Converting to SVG format is not supported. SVG is a vector format.");
            }

            // Handle PDF conversion
            if (outputFormatLower == "pdf")
            {
                return await ConvertToPdfAsync(inputFilePath, outputFilePath, progress);
            }

            // Handle HEIC/JFIF with System.Drawing fallback
            if (inputExtension == ".heic" || inputExtension == ".jfif")
            {
                return await ConvertWithSystemDrawingAsync(inputFilePath, outputFilePath, outputFormatLower, progress);
            }

            // Use ImageSharp for standard conversions
            return await ConvertWithImageSharpAsync(inputFilePath, outputFilePath, outputFormatLower, progress);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Image conversion failed: {ex.Message}", ex);
        }
    }

    private async Task<string> ConvertWithImageSharpAsync(string inputPath, string outputPath, string outputFormat, IProgress<int>? progress)
    {
        progress?.Report(25);

        using var image = await SixLabors.ImageSharp.Image.LoadAsync(inputPath);
        
        progress?.Report(50);

        var encoder = GetImageSharpEncoder(outputFormat);
        
        progress?.Report(75);

        await image.SaveAsync(outputPath, encoder);
        
        progress?.Report(100);
        return outputPath;
    }

    private async Task<string> ConvertWithSystemDrawingAsync(string inputPath, string outputPath, string outputFormat, IProgress<int>? progress)
    {
        progress?.Report(25);

        using var bitmap = new System.Drawing.Bitmap(inputPath);
        
        progress?.Report(50);

        var imageFormat = GetSystemDrawingFormat(outputFormat);
        
        progress?.Report(75);

        await Task.Run(new Action(() => bitmap.Save(outputPath, imageFormat)));
        
        progress?.Report(100);
        return outputPath;
    }

    private async Task<string> ConvertFromSvgAsync(string inputPath, string outputPath, string outputFormat, IProgress<int>? progress)
    {
        progress?.Report(25);

        var svgDocument = SvgDocument.Open(inputPath);
        
        progress?.Report(50);

        using System.Drawing.Bitmap bitmap = svgDocument.Draw();
        
        progress?.Report(75);

        var imageFormat = GetSystemDrawingFormat(outputFormat);
        await Task.Run(new Action(() => bitmap.Save(outputPath, imageFormat)));
        
        progress?.Report(100);
        return outputPath;
    }

    private async Task<string> ConvertToPdfAsync(string inputPath, string outputPath, IProgress<int>? progress)
    {
        progress?.Report(25);

        var imageBytes = await File.ReadAllBytesAsync(inputPath);
        
        progress?.Report(50);

        await Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Content().Image(imageBytes).FitArea();
                });
            }).GeneratePdf(outputPath);
        });
        
        progress?.Report(100);
        return outputPath;
    }

    private SixLabors.ImageSharp.Formats.IImageEncoder GetImageSharpEncoder(string format)
    {
        return format switch
        {
            "png" => new PngEncoder(),
            "jpg" or "jpeg" => new JpegEncoder { Quality = 90 },
            "webp" => new WebpEncoder { Quality = 90 },
            "bmp" => new BmpEncoder(),
            "gif" => new GifEncoder(),
            "tiff" => new TiffEncoder(),
            _ => new PngEncoder()
        };
    }

    private System.Drawing.Imaging.ImageFormat GetSystemDrawingFormat(string format)
    {
        return format switch
        {
            "png" => System.Drawing.Imaging.ImageFormat.Png,
            "jpg" or "jpeg" => System.Drawing.Imaging.ImageFormat.Jpeg,
            "bmp" => System.Drawing.Imaging.ImageFormat.Bmp,
            "gif" => System.Drawing.Imaging.ImageFormat.Gif,
            "tiff" => System.Drawing.Imaging.ImageFormat.Tiff,
            _ => System.Drawing.Imaging.ImageFormat.Png
        };
    }
}