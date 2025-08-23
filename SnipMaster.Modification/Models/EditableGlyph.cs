using UglyToad.PdfPig.Core;

namespace SnipMaster.Modification.Models;

public record EditableGlyph(
    char Character,
    string FontName,
    double PointSize,
    SimpleColor Color,
    PdfRectangle OriginalBoundingBox,
    bool IsBold,
    bool IsItalic
);