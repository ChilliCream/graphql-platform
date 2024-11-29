using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types;

public sealed class Lookup
{
    public Lookup(
        string schemaName,
        string name,
        LookupKind kind,
        ImmutableArray<LookupArgument> arguments,
        ImmutableArray<FieldPath> fields)
    {
        SchemaName = schemaName;
        Kind = kind;
        Arguments = arguments;
        Fields = fields;
        Name = name;
    }

    /// <summary>
    /// Gets the name of the source schema that has requirements. for a field.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the name of the lookup field.
    /// </summary>
    public string Name { get; }

    public LookupKind Kind { get; }

    /// <summary>
    /// Gets the arguments that represent field requirements.
    /// </summary>
    public ImmutableArray<LookupArgument> Arguments { get; }

    /// <summary>
    /// Gets the paths to the field that are required.
    /// </summary>
    public ImmutableArray<FieldPath> Fields { get; }
}
