namespace SnipMaster.Conversion.Services;

public class ConversionServiceFactory : IConversionServiceFactory
{
    private readonly IFileConversionService _fileConversionService;
    private readonly IImageConversionService _imageConversionService;
    private readonly IDocumentConversionService _documentConversionService;

    private readonly string[] _videoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".flv", ".m4v" };
    private readonly string[] _audioExtensions = { ".mp3", ".ogg", ".wav", ".aac", ".flac", ".m4a", ".wma" };
    private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".heic", ".jfif", ".svg" };
    private readonly string[] _documentExtensions = { ".pdf", ".docx", ".doc", ".epub", ".txt", ".rtf" };

    public ConversionServiceFactory(IFileConversionService fileConversionService, IImageConversionService imageConversionService, IDocumentConversionService documentConversionService)
    {
        _fileConversionService = fileConversionService;
        _imageConversionService = imageConversionService;
        _documentConversionService = documentConversionService;
    }

    public IFileConversionService GetConversionService(string filePath)
    {
        var serviceType = GetServiceType(filePath);
        
        return serviceType switch
        {
            ConversionServiceType.VideoAudio => _fileConversionService,
            ConversionServiceType.Image => (IFileConversionService)_imageConversionService,
            ConversionServiceType.Document => (IFileConversionService)_documentConversionService,
            ConversionServiceType.Unsupported => throw new NotSupportedException($"File type not supported: {Path.GetExtension(filePath)}"),
            _ => throw new ArgumentException("Invalid service type")
        };
    }
    
    public IImageConversionService GetImageConversionService(string filePath)
    {
        var serviceType = GetServiceType(filePath);
        
        if (serviceType == ConversionServiceType.Image)
            return _imageConversionService;
            
        throw new NotSupportedException($"File is not an image: {Path.GetExtension(filePath)}");
    }
    
    public IDocumentConversionService GetDocumentConversionService(string filePath)
    {
        var serviceType = GetServiceType(filePath);
        
        if (serviceType == ConversionServiceType.Document)
            return _documentConversionService;
            
        throw new NotSupportedException($"File is not a document: {Path.GetExtension(filePath)}");
    }

    public ConversionServiceType GetServiceType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();

        if (_videoExtensions.Contains(extension) || _audioExtensions.Contains(extension))
            return ConversionServiceType.VideoAudio;
        
        if (_imageExtensions.Contains(extension))
            return ConversionServiceType.Image;
        
        if (_documentExtensions.Contains(extension))
            return ConversionServiceType.Document;
        
        return ConversionServiceType.Unsupported;
    }

    public bool IsConversionSupported(string fromPath, string toFormat)
    {
        var fromType = GetServiceType(fromPath);
        var toExtension = $".{toFormat.TrimStart('.')}";
        
        return fromType switch
        {
            ConversionServiceType.VideoAudio => _videoExtensions.Contains(toExtension) || _audioExtensions.Contains(toExtension),
            ConversionServiceType.Image => _imageExtensions.Contains(toExtension),
            ConversionServiceType.Document => _documentExtensions.Contains(toExtension),
            _ => false
        };
    }

    public string[] GetSupportedFormats(ConversionServiceType serviceType)
    {
        return serviceType switch
        {
            ConversionServiceType.VideoAudio => _videoExtensions.Concat(_audioExtensions).Select(ext => ext.TrimStart('.')).ToArray(),
            ConversionServiceType.Image => _imageConversionService.GetSupportedFormats(),
            ConversionServiceType.Document => _documentConversionService.GetSupportedFormats(),
            _ => Array.Empty<string>()
        };
    }
}