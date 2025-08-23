using System.Diagnostics;
using SnipMaster.Modification.Models;
using SnipMaster.Modification.Enums;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics.Colors;

namespace SnipMaster.Modification.Services;

public class PdfParserService
{
    public (List<LivePage> Pages, FontMetricsCacheService MetricsCache) ParseDocument(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        var livePages = new List<LivePage>();
        var metricsCache = new FontMetricsCacheService();

        foreach (var page in document.GetPages())
        {
            foreach (var letter in page.Letters)
            {
                metricsCache.StoreMetric(letter.FontName, letter.Value[0], letter.GlyphRectangle);
            }
            
            var paragraphs = GroupLettersIntoParagraphs(page.Letters.ToList());
            var images = page.GetImages().ToList();

            livePages.Add(new LivePage(page.Number, page.Width, page.Height, paragraphs, images));
        }
        Debug.WriteLine($"[Parser] SUCCESS: Parsed {livePages.Count} pages. Found {metricsCache} unique fonts.");
        return (livePages, metricsCache);
    }

    private List<LiveParagraph> GroupLettersIntoParagraphs(List<Letter> letters)
    {
        if (letters == null || !letters.Any()) return new List<LiveParagraph>();

        var lines = letters.GroupBy(l => Math.Round(l.StartBaseLine.Y, 1))
                           .OrderByDescending(g => g.Key)
                           .Select(g => g.OrderBy(l => l.StartBaseLine.X).ToList())
                           .Where(l => l.Any())
                           .ToList();

        if (!lines.Any()) return new List<LiveParagraph>();

        var paragraphLetterGroups = new List<List<Letter>>();
        var currentParagraphGroup = new List<Letter>(lines[0]);

        for (int i = 1; i < lines.Count; i++)
        {
            var prevLine = lines[i - 1];
            var currentLine = lines[i];

            bool isParagraphBreak = false;

            double prevLineMaxHeight = prevLine.Max(l => l.GlyphRectangle.Height);
            double verticalGap = prevLine[0].StartBaseLine.Y - currentLine[0].StartBaseLine.Y;
            if (verticalGap > prevLineMaxHeight * 1.8)
            {
                isParagraphBreak = true;
            }

            if (!isParagraphBreak)
            {
                double paragraphMargin = GetAverageLeft(currentParagraphGroup);
                double indentAmount = currentLine[0].StartBaseLine.X - paragraphMargin;
                double avgCharWidth = currentParagraphGroup.Any() ? currentParagraphGroup.Average(l => l.GlyphRectangle.Width) : 10;
                if (indentAmount > avgCharWidth)
                {
                    isParagraphBreak = true;
                }
            }
            
            if (!isParagraphBreak)
            {
                double prevParagraphPointSize = GetMaxPointSize(currentParagraphGroup);
                double currentLinePointSize = currentLine.Max(l => l.PointSize);
                if (currentLinePointSize > prevParagraphPointSize + 1 ||
                    currentLine[0].FontName != prevLine[0].FontName)
                {
                    isParagraphBreak = true;
                }
            }

            if (isParagraphBreak)
            {
                paragraphLetterGroups.Add(currentParagraphGroup);
                currentParagraphGroup = new List<Letter>(currentLine);
            }
            else
            {
                currentParagraphGroup.AddRange(currentLine);
            }
        }
        paragraphLetterGroups.Add(currentParagraphGroup);

        return ConvertLetterGroupsToLiveParagraphs(paragraphLetterGroups);
    }

    private List<LiveParagraph> ConvertLetterGroupsToLiveParagraphs(List<List<Letter>> paragraphLetterGroups)
    {
        var liveParagraphs = new List<LiveParagraph>();
        foreach (var letterGroup in paragraphLetterGroups)
        {
            var editableGlyphs = letterGroup.Select(l => new EditableGlyph(
                l.Value[0],
                l.FontName,
                l.PointSize,
                ConvertColor(l.Color),
                l.GlyphRectangle,
                l.Font.IsBold ,
                l.Font.IsItalic
            )).ToList();

            var boundingBox = CalculateBoundingBox(letterGroup);
            var alignment = CalculateAlignment(letterGroup, boundingBox);

            liveParagraphs.Add(new LiveParagraph(editableGlyphs, boundingBox, alignment));
        }
        return liveParagraphs;
    }

    private double GetAverageLeft(List<Letter> letters)
    {
        if (letters == null || !letters.Any()) return 0;
        var lineLefts = letters.GroupBy(l => Math.Round(l.StartBaseLine.Y, 1))
                               .Select(g => g.Min(l => l.StartBaseLine.X));
        return lineLefts.Any() ? lineLefts.Average() : 0;
    }

    private double GetMaxPointSize(List<Letter> letters)
    {
        if (letters == null || !letters.Any()) return 0;
        return letters.Max(l => l.PointSize);
    }

    private double GetAverageHorizontalCenter(List<Letter> letters)
    {
        if (letters == null || !letters.Any()) return 0;
        return (letters.Min(l => l.GlyphRectangle.Left) + letters.Max(l => l.GlyphRectangle.Right)) / 2;
    }

    private double GetAverageWidth(List<Letter> letters)
    {
        if (letters == null || !letters.Any()) return 0;
        var lines = letters.GroupBy(l => Math.Round(l.StartBaseLine.Y, 1));
        return lines.Any() ? lines.Average(g => g.Max(l => l.GlyphRectangle.Right) - g.Min(l => l.GlyphRectangle.Left)) : 0;
    }
    private SimpleColor ConvertColor(IColor color)
    {
        return color.ColorSpace switch
        {
            ColorSpace.DeviceRGB => new SimpleColor { R = color.ToRGBValues().r, G = color.ToRGBValues().g, B = color.ToRGBValues().b },
            ColorSpace.DeviceGray => new SimpleColor { R = color.ToRGBValues().r, G = color.ToRGBValues().r, B = color.ToRGBValues().r },
            _ => new SimpleColor { R = color.ToRGBValues().r, G = color.ToRGBValues().g, B = color.ToRGBValues().b }
        };
    }

    private PdfRectangle CalculateBoundingBox(List<Letter> letters)
    {
        if (!letters.Any()) return new PdfRectangle(0, 0, 0, 0);

        var minX = letters.Min(l => l.GlyphRectangle.Left);
        var minY = letters.Min(l => l.GlyphRectangle.Bottom);
        var maxX = letters.Max(l => l.GlyphRectangle.Right);
        var maxY = letters.Max(l => l.GlyphRectangle.Top);

        return new PdfRectangle(minX, minY, maxX, maxY);
    }

    private TextJustification CalculateAlignment(List<Letter> letters, PdfRectangle boundingBox)
    {
        var lines = letters.GroupBy(l => Math.Round(l.GlyphRectangle.Bottom, 1)).ToList();
        if (lines.Count < 2) return TextJustification.Left;

        var leftPositions = lines.Select(line => line.Min(l => l.GlyphRectangle.Left)).ToList();
        var rightPositions = lines.Select(line => line.Max(l => l.GlyphRectangle.Right)).ToList();

        var leftAligned = leftPositions.All(pos => Math.Abs(pos - leftPositions.First()) < 1);
        var rightAligned = rightPositions.All(pos => Math.Abs(pos - rightPositions.First()) < 1);

        if (leftAligned && rightAligned) return TextJustification.Justified;
        if (rightAligned) return TextJustification.Right;
        
        var centerPositions = lines.Select(line => (line.Min(l => l.GlyphRectangle.Left) + line.Max(l => l.GlyphRectangle.Right)) / 2).ToList();
        var paragraphCenter = (boundingBox.Left + boundingBox.Right) / 2;
        var centered = centerPositions.All(pos => Math.Abs(pos - paragraphCenter) < 1);
        
        return centered ? TextJustification.Center : TextJustification.Left;
    }
}