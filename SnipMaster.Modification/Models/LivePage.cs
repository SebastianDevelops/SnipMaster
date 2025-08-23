using UglyToad.PdfPig.Content;

namespace SnipMaster.Modification.Models;

public record LivePage(
    int PageNumber,
    double Width,
    double Height,
    List<LiveParagraph> Paragraphs,
    List<UglyToad.PdfPig.Content.IPdfImage> Images
);