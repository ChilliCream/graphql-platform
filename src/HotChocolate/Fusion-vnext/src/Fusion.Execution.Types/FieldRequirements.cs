using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types;

public sealed class FieldRequirements(
    string schemaName,
    ImmutableArray<RequiredArgument> arguments,
    ImmutableArray<FieldPath> fields,
    SelectionSetNode selectionSet)
{
    /// <summary>
    /// Gets the name of the source schema that has requirements. for a field.
    /// </summary>
    public string SchemaName { get; } = schemaName;

    /// <summary>
    /// Gets the arguments that represent field requirements.
    /// </summary>
    public ImmutableArray<RequiredArgument> Arguments { get; } = arguments;

    /// <summary>
    /// Gets the paths to the field that are required.
    /// </summary>
    public ImmutableArray<FieldPath> Fields { get; } = fields;

    /// <summary>
    /// Gets the selection set that represents the field requirements.
    /// </summary>
    public SelectionSetNode SelectionSet { get; } = selectionSet;
}
