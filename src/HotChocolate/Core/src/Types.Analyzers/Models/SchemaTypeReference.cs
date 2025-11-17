namespace HotChocolate.Types.Analyzers.Models;

public readonly struct SchemaTypeReference
{
    public SchemaTypeReference(
        SchemaTypeReferenceKind kind,
        string typeString,
        string? typeKey = null,
        string? typeDefinitionString = null)
    {
        Kind = kind;
        TypeString = typeString;
        TypeKey = typeKey;
        TypeDefinitionString = typeDefinitionString;
    }

    public SchemaTypeReferenceKind Kind { get; }

    public string TypeString { get; }

    public string? TypeKey { get; }

    public string? TypeDefinitionString { get; }
}
