namespace HotChocolate.Types.Analyzers.Models;

public readonly struct SchemaTypeReference
{
    public SchemaTypeReference(SchemaTypeReferenceKind kind, string typeString)
    {
        Kind = kind;
        TypeString = typeString;
    }

    public SchemaTypeReferenceKind Kind { get; }

    public string TypeString { get; }
}
