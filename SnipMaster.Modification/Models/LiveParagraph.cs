using SnipMaster.Modification.Enums;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace SnipMaster.Modification.Models;

public record LiveParagraph(
    List<EditableGlyph> Glyphs,
    PdfRectangle BoundingBox,
    TextJustification Alignment
);