using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning.Directives;

public sealed class Requirement(
    string schemaName,
    ImmutableArray<RequiredArgument> arguments,
    ImmutableArray<RequiredField> fields)
{
    /// <summary>
    /// Gets the name of the source schema that has requirements. for a field.
    /// </summary>
    public string SchemaName { get; } = schemaName;

    /// <summary>
    /// Gets the arguments
    /// </summary>
    public ImmutableArray<RequiredArgument> Arguments { get; } = arguments;

    /// <summary>
    /// Gets the paths to the field that are required.
    /// </summary>
    public ImmutableArray<RequiredField> Fields { get; } = fields;
}
