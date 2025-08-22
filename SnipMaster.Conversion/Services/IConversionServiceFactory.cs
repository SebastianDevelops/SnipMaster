namespace SnipMaster.Conversion.Services;

public interface IConversionServiceFactory
{
    IFileConversionService GetConversionService(string filePath);
    IImageConversionService GetImageConversionService(string filePath);
    IDocumentConversionService GetDocumentConversionService(string filePath);
    ConversionServiceType GetServiceType(string filePath);
    string[] GetSupportedFormats(ConversionServiceType serviceType);
    bool IsConversionSupported(string fromPath, string toFormat);
}

public enum ConversionServiceType
{
    VideoAudio,
    Image,
    Document,
    Unsupported
}