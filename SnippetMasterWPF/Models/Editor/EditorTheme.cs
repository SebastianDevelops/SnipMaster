namespace SnippetMasterWPF.Models.Editor;

[Serializable]
public class EditorTheme
{
    public string? Base { get; init; }

    public bool Inherit { get; init; }

    public IDictionary<string, string>? Rules { get; init; }

    public IDictionary<string, string>? Colors { get; init; }
}