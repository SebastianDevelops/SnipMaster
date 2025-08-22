using DocumentFormat.OpenXml.Packaging;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using OpenXmlPowerTools;
using System.Drawing;
using HtmlAgilityPack;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PDFtoImage;

namespace SnipMaster.Conversion.Services;

public class DocumentConversionService : IDocumentConversionService
{
    private readonly string[] _supportedExtensions = { ".pdf", ".docx", ".doc", ".epub", ".txt", ".rtf", ".jpg", ".jpeg", ".png", ".heic" };

    public bool IsDocumentFile(string filePath) =>
        _supportedExtensions.Contains(System.IO.Path.GetExtension(filePath).ToLower());

    public string[] GetSupportedFormats() =>
        new[] { "pdf", "docx", "epub", "txt", "png", "jpg", "jpeg" };

    public async Task<string> ConvertDocumentAsync(string inputFilePath, string outputFormat, string? outputFilePath = null, IProgress<int>? progress = null)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        outputFilePath ??= System.IO.Path.ChangeExtension(inputFilePath, outputFormat);
        var inputExtension = System.IO.Path.GetExtension(inputFilePath).ToLower();
        var outputFormatLower = outputFormat.ToLower();

        progress?.Report(10);

        try
        {
            return await ExecuteConversionAsync(inputFilePath, outputFilePath, inputExtension, outputFormatLower, progress);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert '{inputExtension}' to '{outputFormatLower}': {ex.Message}", ex);
        }
    }

    private async Task<string> ExecuteConversionAsync(string inputPath, string outputPath, string inputExt, string outputFormat, IProgress<int>? progress)
    {
        return (inputExt, outputFormat) switch
        {
            (".docx", "pdf") or (".doc", "pdf") or (".xls", "pdf") or (".xlsx", "pdf") or (".ppt", "pdf") or (".pptx", "pdf") => await OfficeToPdfAsync(inputPath, outputPath, progress),
            (".docx", "txt") => await DocxToTxtAsync(inputPath, outputPath, progress),
            (".epub", "txt") => await EpubToTxtAsync(inputPath, outputPath, progress),
            (".pdf", "png") or (".pdf", "jpg") or (".pdf", "jpeg") => await PdfToImageAsync(inputPath, outputPath, outputFormat, progress),
            _ => throw new NotSupportedException($"Conversion from {inputExt} to {outputFormat} is not supported")
        };
    }

    private async Task<string> DocxToTxtAsync(string inputPath, string outputPath, IProgress<int>? progress)
    {
        await Task.Run(() =>
        {
            progress?.Report(25);
            
            byte[] byteArray = File.ReadAllBytes(inputPath);
            using var memoryStream = new MemoryStream();
            memoryStream.Write(byteArray, 0, byteArray.Length);
            using var wordDocument = WordprocessingDocument.Open(memoryStream, true);
            
            progress?.Report(50);
            
            var settings = new WmlToHtmlConverterSettings();
            var html = WmlToHtmlConverter.ConvertToHtml(wordDocument, settings);
            
            progress?.Report(75);
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html.ToString());
            var text = doc.DocumentNode.InnerText;
            
            File.WriteAllText(outputPath, text);
            
            progress?.Report(100);
        });
        return outputPath;
    }

    private async Task<string> EpubToTxtAsync(string inputPath, string outputPath, IProgress<int>? progress)
    {
        progress?.Report(25);
        var textContent = new StringBuilder();

        using var archive = ZipFile.OpenRead(inputPath);
        var containerEntry = archive.GetEntry("META-INF/container.xml");
        if (containerEntry != null)
        {
            using var stream = containerEntry.Open();
            var containerDoc = XDocument.Load(stream);
            var ns = containerDoc.Root.Name.Namespace;
            var rootFilePath = containerDoc.Descendants(ns + "rootfile").First().Attribute("full-path").Value;
            
            progress?.Report(50);
            
            var opfEntry = archive.GetEntry(rootFilePath);
            if (opfEntry != null)
            {
                using var opfStream = opfEntry.Open();
                var opfDoc = XDocument.Load(opfStream);
                var opfNs = opfDoc.Root.Name.Namespace;
                var itemrefs = opfDoc.Descendants(opfNs + "itemref").Select(ir => ir.Attribute("idref").Value);
                var items = opfDoc.Descendants(opfNs + "item")
                                 .ToDictionary(item => item.Attribute("id").Value, item => item.Attribute("href").Value);

                var opfDir = System.IO.Path.GetDirectoryName(rootFilePath).Replace('\\', '/');
                foreach (var idref in itemrefs)
                {
                    if (items.TryGetValue(idref, out var href))
                    {
                        var contentPath = System.IO.Path.Combine(opfDir, href).Replace('\\', '/');
                        var contentEntry = archive.GetEntry(contentPath);
                        if (contentEntry != null)
                        {
                            using var reader = new StreamReader(contentEntry.Open());
                            var htmlContent = await reader.ReadToEndAsync();
                            
                            var doc = new HtmlDocument();
                            doc.LoadHtml(htmlContent);
                            textContent.AppendLine(doc.DocumentNode.InnerText);
                        }
                    }
                }
            }
        }

        progress?.Report(75);
        
        await File.WriteAllTextAsync(outputPath, textContent.ToString());
        
        progress?.Report(100);
        return outputPath;
    }

    private async Task<string> OfficeToPdfAsync(string inputPath, string outputPath, IProgress<int>? progress)
    {
        await Task.Run(() =>
        {
            progress?.Report(25);
            
            byte[] byteArray = File.ReadAllBytes(inputPath);
            using var memoryStream = new MemoryStream();
            memoryStream.Write(byteArray, 0, byteArray.Length);
            using var wordDocument = WordprocessingDocument.Open(memoryStream, true);
            
            progress?.Report(50);
            
            var settings = new WmlToHtmlConverterSettings();
            var html = WmlToHtmlConverter.ConvertToHtml(wordDocument, settings);
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html.ToString());
            var text = doc.DocumentNode.InnerText;
            
            progress?.Report(75);
            
            QuestPDF.Settings.License = LicenseType.Community;
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Content().Text(text).FontSize(12);
                });
            }).GeneratePdf(outputPath);
            
            progress?.Report(100);
        });
        return outputPath;
    }

    private async Task<string> PdfToImageAsync(string inputPath, string outputPath, string outputFormat, IProgress<int>? progress)
    {
        progress?.Report(25);
        
        // Simple PDF to image conversion using first page
        // Note: This is a basic implementation. For production, consider using a dedicated PDF library
        await Task.Run(() =>
        {
            progress?.Report(50);
            
            // Convert first page of PDF to image using PDFtoImage direct save methods
            switch (outputFormat.ToLower())
            {
                case "png":
                    PDFtoImage.Conversion.SavePng(outputPath, inputPath, page: 0);
                    break;
                case "jpg" or "jpeg":
                    PDFtoImage.Conversion.SaveJpeg(outputPath, inputPath, page: 0);
                    break;
                default:
                    PDFtoImage.Conversion.SavePng(outputPath, inputPath, page: 0);
                    break;
            }
            
            progress?.Report(100);
        });
        
        return outputPath;
    }

    public async Task<string> ConvertPdfToImageAsync(string inputFilePath, string outputFormat, string outputFilePath, int pageNumber, string? password = null, IProgress<int>? progress = null)
    {
        progress?.Report(25);
        
        await Task.Run(() =>
        {
            progress?.Report(50);
            
            try
            {
                var pdfBytes = File.ReadAllBytes(inputFilePath);
                switch (outputFormat.ToLower())
                {
                    case "png":
                        PDFtoImage.Conversion.SavePng(outputFilePath, pdfBytes, page: pageNumber, password: password);
                        break;
                    case "jpg" or "jpeg":
                        PDFtoImage.Conversion.SaveJpeg(outputFilePath, pdfBytes, page: pageNumber, password: password);
                        break;
                    default:
                        PDFtoImage.Conversion.SavePng(outputFilePath, pdfBytes, page: pageNumber, password: password);
                        break;
                }
            }
            catch (Exception ex) when (ex.Message.Contains("password") || ex.Message.Contains("encrypted"))
            {
                throw new UnauthorizedAccessException("PDF requires a password to access.");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException($"Page {pageNumber + 1} does not exist in the PDF.");
            }
            
            progress?.Report(100);
        });
        
        return outputFilePath;
    }

    public int GetPdfPageCount(string pdfFilePath, string? password = null)
    {
        try
        {
            var pdfBytes = File.ReadAllBytes(pdfFilePath);
            return PDFtoImage.Conversion.GetPageCount(pdfBytes, password: password);
        }
        catch (Exception ex) when (ex.Message.Contains("password") || ex.Message.Contains("encrypted"))
        {
            throw new UnauthorizedAccessException("PDF requires a password to access.");
        }
    }

    private bool IsImageExtension(string extension)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".heic", ".jfif", ".bmp", ".gif" };
        return imageExtensions.Contains(extension.ToLower());
    }
}