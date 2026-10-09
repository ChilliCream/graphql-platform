namespace HotChocolate.Types.Analyzers.Models;

public readonly struct SchemaTypeReference
{
    public SchemaTypeReference(
        SchemaTypeReferenceKind kind,
        string typeString,
        string? typeStructure = null,
        string? nullability = null,
        bool nonNull = false)
    {
        Kind = kind;
        TypeString = typeString;
        TypeStructure = typeStructure;
        Nullability = nullability;
        NonNull = nonNull;
    }

    public SchemaTypeReferenceKind Kind { get; }

    public string TypeString { get; }

    public string? TypeStructure { get; }

    public string? Nullability { get; }

    public bool NonNull { get; }
}
