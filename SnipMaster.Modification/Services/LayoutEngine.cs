using SnipMaster.Modification.Models;
using UglyToad.PdfPig.Core;

namespace SnipMaster.Modification.Services;

public record PositionedGlyph(
    char Character,
    PdfRectangle CalculatedBoundingBox,
    string FontName,
    double PointSize,
    SimpleColor Color
);

public record PositionedLine(
    List<PositionedGlyph> Glyphs,
    double BaseLineY
);

public class LayoutEngine
{
    private readonly LiveParagraph _paragraph;
    private readonly FontMetricsCacheService _metricsCache;
    private List<PositionedLine> _calculatedLayout = new();

    public LayoutEngine(LiveParagraph paragraph, FontMetricsCacheService metricsCache)
    {
        _paragraph = paragraph;
        _metricsCache = metricsCache;
        Reflow();
    }

    public IReadOnlyList<PositionedLine> GetLayout() => _calculatedLayout;

    public void DeleteGlyphs(int index, int count)
    {
        _paragraph.Glyphs.RemoveRange(index, count);
        Reflow();
    }

    public void InsertText(int index, string text)
    {
        if (index < 0 || index > _paragraph.Glyphs.Count) return;

        EditableGlyph templateStyleGlyph = null;
        if (_paragraph.Glyphs.Any())
        {
            templateStyleGlyph = (index < _paragraph.Glyphs.Count) 
                               ? _paragraph.Glyphs[index] 
                               : _paragraph.Glyphs.Last();
        }
        else
        {
            return;
        }

        var newGlyphs = new List<EditableGlyph>();
        foreach (char ch in text)
        {
            PdfRectangle newGlyphBbox;
            
            if (!_metricsCache.TryGetBoundingBox(templateStyleGlyph.FontName, ch, out newGlyphBbox))
            {
                newGlyphBbox = templateStyleGlyph.OriginalBoundingBox;
            }

            newGlyphs.Add(new EditableGlyph(
                ch,
                templateStyleGlyph.FontName,
                templateStyleGlyph.PointSize,
                templateStyleGlyph.Color,
                newGlyphBbox,
                templateStyleGlyph.IsBold,
                templateStyleGlyph.IsItalic,
                templateStyleGlyph.AdvanceWidth
            ));
        }

        _paragraph.Glyphs.InsertRange(index, newGlyphs);
        Reflow();
    }

    private void Reflow()
    {
        _calculatedLayout.Clear();
        var box = _paragraph.BoundingBox;
        if (!_paragraph.Glyphs.Any()) return;

        double currentY = box.Top;
        double currentX = box.Left;
        var currentLineGlyphs = new List<PositionedGlyph>();
        double averageLineHeight = _paragraph.Glyphs.First().PointSize * 1.2;

        var words = GroupGlyphsIntoWords(_paragraph.Glyphs);

        foreach (var word in words)
        {
            double wordWidth = word.Sum(g => g.AdvanceWidth);

            if (currentX > box.Left && currentX + wordWidth > box.Right)
            {
                if (currentLineGlyphs.Any())
                {
                    _calculatedLayout.Add(new PositionedLine(currentLineGlyphs, currentY));
                }
                
                currentX = box.Left;
                currentY -= averageLineHeight;
                currentLineGlyphs = new List<PositionedGlyph>();
            }

            double glyphX = currentX;
            foreach (var glyph in word)
            {
                var newBounds = new PdfRectangle(
                    glyphX,
                    currentY - glyph.OriginalBoundingBox.Height,
                    glyphX + glyph.OriginalBoundingBox.Width,
                    currentY
                );

                currentLineGlyphs.Add(new PositionedGlyph(
                    glyph.Character, newBounds, glyph.FontName, glyph.PointSize, glyph.Color
                ));
                
                glyphX += glyph.AdvanceWidth;
            }
            
            currentX += wordWidth;
        }

        if (currentLineGlyphs.Any())
        {
            _calculatedLayout.Add(new PositionedLine(currentLineGlyphs, currentY));
        }
    }

    private List<List<EditableGlyph>> GroupGlyphsIntoWords(List<EditableGlyph> glyphs)
    {
        var words = new List<List<EditableGlyph>>();
        var currentWord = new List<EditableGlyph>();
        
        foreach (var glyph in glyphs)
        {
            if (glyph.Character == ' ')
            {
                if (currentWord.Any())
                {
                    words.Add(currentWord);
                    currentWord = new List<EditableGlyph>();
                }
                words.Add(new List<EditableGlyph> { glyph });
            }
            else
            {
                currentWord.Add(glyph);
            }
        }
        
        if (currentWord.Any())
            words.Add(currentWord);
            
        return words;
    }
}