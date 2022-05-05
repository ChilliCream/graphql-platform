namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class GeneratorDocument
{
    public GeneratorDocument(
        string name,
        string sourceText,
        GeneratorDocumentKind kind,
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

    public GeneratorDocumentKind Kind { get; }

    public string? Hash { get; }

    public string? Path { get; }
}
