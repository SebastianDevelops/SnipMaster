namespace SnipMasterLib.Models;

public class ClipboardEntry
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ContentType { get; set; } = "text";
}