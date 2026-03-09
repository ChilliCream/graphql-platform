namespace HotChocolate.Types.Analyzers.Models;

public readonly struct SchemaTypeReference
{
    public SchemaTypeReference(
        SchemaTypeReferenceKind kind,
        string typeString,
        string? typeStructure = null)
    {
        Kind = kind;
        TypeString = typeString;
        TypeStructure = typeStructure;
    }

    public SchemaTypeReferenceKind Kind { get; }

    public string TypeString { get; }

    public string? TypeStructure { get; }
}
