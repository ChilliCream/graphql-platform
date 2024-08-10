using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types;

public sealed class Lookup(
    string schemaName,
    LookupKind kind,
    ImmutableArray<LookupArgument> arguments,
    ImmutableArray<FieldPath> fields)
{
    /// <summary>
    /// Gets the name of the source schema that has requirements. for a field.
    /// </summary>
    public string SchemaName { get; } = schemaName;

    public LookupKind Kind { get; } = kind;

    /// <summary>
    /// Gets the arguments that represent field requirements.
    /// </summary>
    public ImmutableArray<LookupArgument> Arguments { get; } = arguments;

    /// <summary>
    /// Gets the paths to the field that are required.
    /// </summary>
    public ImmutableArray<FieldPath> Fields { get; } = fields;
}
