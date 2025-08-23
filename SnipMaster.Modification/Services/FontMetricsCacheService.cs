using UglyToad.PdfPig.Core;

namespace SnipMaster.Modification.Services;

public class FontMetricsCacheService
{
    private readonly Dictionary<string, Dictionary<char, PdfRectangle>> _cache = new();

    public void StoreMetric(string fontName, char character, PdfRectangle boundingBox)
    {
        if (!_cache.ContainsKey(fontName))
        {
            _cache[fontName] = new Dictionary<char, PdfRectangle>();
        }
        if (!_cache[fontName].ContainsKey(character))
        {
             _cache[fontName][character] = boundingBox;
        }
    }

    public bool TryGetBoundingBox(string fontName, char character, out PdfRectangle boundingBox)
    {
        boundingBox = default;
        if (_cache.TryGetValue(fontName, out var fontMetrics))
        {
            if (fontMetrics.TryGetValue(character, out var box))
            {
                boundingBox = box;
                return true;
            }
        }
        return false;
    }
}