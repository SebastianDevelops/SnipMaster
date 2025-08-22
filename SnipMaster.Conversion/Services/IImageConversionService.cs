namespace SnipMaster.Conversion.Services;

public interface IImageConversionService
{
    Task<string> ConvertImageAsync(string inputFilePath, string outputFormat, string? outputFilePath = null, IProgress<int>? progress = null);
    bool IsImageFile(string filePath);
    string[] GetSupportedFormats();
}

public static class ImageFormats
{
    public const string PNG = "png";
    public const string JPG = "jpg";
    public const string JPEG = "jpeg";
    public const string WEBP = "webp";
    public const string BMP = "bmp";
    public const string GIF = "gif";
    public const string TIFF = "tiff";
    public const string SVG = "svg";
    public const string HEIC = "heic";
    public const string JFIF = "jfif";
}