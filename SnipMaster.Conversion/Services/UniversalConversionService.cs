namespace SnipMaster.Conversion.Services;

public class UniversalConversionService : IUniversalConversionService
{
    private readonly IConversionServiceFactory _factory;

    public UniversalConversionService(IConversionServiceFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> ConvertFileAsync(string inputFilePath, string outputFormat, string? outputFilePath = null, IProgress<int>? progress = null)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        if (!IsConversionSupported(inputFilePath, outputFormat))
            throw new NotSupportedException($"Conversion from {Path.GetExtension(inputFilePath)} to {outputFormat} is not supported");

        var serviceType = _factory.GetServiceType(inputFilePath);
        
        return serviceType switch
        {
            ConversionServiceType.VideoAudio => await ConvertVideoAudioAsync(inputFilePath, outputFormat, outputFilePath, progress),
            ConversionServiceType.Image => await ConvertImageAsync(inputFilePath, outputFormat, outputFilePath, progress),
            ConversionServiceType.Document => await ConvertDocumentAsync(inputFilePath, outputFormat, outputFilePath, progress),
            _ => throw new NotSupportedException($"Service type {serviceType} not supported")
        };
    }

    public async Task<ConversionInfo> GetConversionInfoAsync(string inputFilePath, string outputFormat)
    {
        var serviceType = _factory.GetServiceType(inputFilePath);
        var inputFormat = Path.GetExtension(inputFilePath).TrimStart('.');
        var isSupported = IsConversionSupported(inputFilePath, outputFormat);
        var availableFormats = _factory.GetSupportedFormats(serviceType);

        return await Task.FromResult(new ConversionInfo
        {
            ServiceType = serviceType,
            InputFormat = inputFormat,
            OutputFormat = outputFormat,
            IsSupported = isSupported,
            AvailableFormats = availableFormats
        });
    }

    public string[] GetSupportedFormats(string inputFilePath)
    {
        var serviceType = _factory.GetServiceType(inputFilePath);
        return _factory.GetSupportedFormats(serviceType);
    }

    public bool IsConversionSupported(string inputFilePath, string outputFormat)
    {
        try
        {
            var service = _factory.GetConversionService(inputFilePath);
            return service != null;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> ConvertVideoAudioAsync(string inputFilePath, string outputFormat, string? outputFilePath, IProgress<int>? progress)
    {
        var conversionService = (IFileConversionService)_factory.GetConversionService(inputFilePath);
        
        if (conversionService.IsVideoFile(inputFilePath))
        {
            var audioFormats = new[] { "mp3", "ogg", "wav", "aac", "flac" };
            if (audioFormats.Contains(outputFormat.ToLower()))
            {
                return await conversionService.ExtractAudioAsync(inputFilePath, outputFormat, outputFilePath, progress);
            }
            else
            {
                return await conversionService.ConvertVideoAsync(inputFilePath, outputFormat, outputFilePath, progress);
            }
        }
        else if (conversionService.IsAudioFile(inputFilePath))
        {
            return await conversionService.ConvertAudioAsync(inputFilePath, outputFormat, outputFilePath, progress);
        }
        
        throw new NotSupportedException($"File type not supported: {Path.GetExtension(inputFilePath)}");
    }
    
    private async Task<string> ConvertImageAsync(string inputFilePath, string outputFormat, string? outputFilePath, IProgress<int>? progress)
    {
        var imageService = _factory.GetImageConversionService(inputFilePath);
        return await imageService.ConvertImageAsync(inputFilePath, outputFormat, outputFilePath, progress);
    }
    
    private async Task<string> ConvertDocumentAsync(string inputFilePath, string outputFormat, string? outputFilePath, IProgress<int>? progress)
    {
        var documentService = _factory.GetDocumentConversionService(inputFilePath);
        return await documentService.ConvertDocumentAsync(inputFilePath, outputFormat, outputFilePath, progress);
    }
}