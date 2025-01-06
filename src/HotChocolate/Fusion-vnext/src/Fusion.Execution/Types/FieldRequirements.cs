using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Specifies the data requirements of a field for a specific source schema.
/// </summary>
/// <param name="schemaName">
/// The name of the source schema that has requirements for a field.
/// </param>
/// <param name="arguments">
/// The arguments that represent the field requirements.
/// </param>
/// <param name="fields">
/// The paths to the field that are required.
/// </param>
/// <param name="selectionSet">
/// The selection set that represents the field requirements.
/// </param>
public sealed class FieldRequirements(
    string schemaName,
    ImmutableArray<RequiredArgument> arguments,
    ImmutableArray<SelectionPath> fields,
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
    public ImmutableArray<SelectionPath> Fields { get; } = fields;

    /// <summary>
    /// Gets the selection set that represents the field requirements.
    /// </summary>
    public SelectionSetNode SelectionSet { get; } = selectionSet;
}
