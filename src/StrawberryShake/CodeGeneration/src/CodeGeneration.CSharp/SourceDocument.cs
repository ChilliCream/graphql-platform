namespace StrawberryShake.CodeGeneration.CSharp;

public class SourceDocument
{
    public SourceDocument(
        string name,
        string sourceText,
        SourceDocumentKind kind,
        string? hash = null,
        string? path = null)
    {
        Name = name;
        SourceText = sourceText;
        Kind = kind;
        Hash = hash;
        Path = path;
    }

    public string Name { get; }

    public string SourceText { get; }

    public SourceDocumentKind Kind { get; }

    public string? Hash { get; }

    public string? Path { get; }
}
