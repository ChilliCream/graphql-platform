using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types;

public sealed class Lookup
{
    public Lookup(
        string schemaName,
        string name,
        ImmutableArray<LookupArgument> arguments,
        ImmutableArray<SelectionPath> fields,
        SelectionSetNode selectionSet)
    {
        SchemaName = schemaName;
        Name = name;
        Arguments = arguments;
        Fields = fields;
        SelectionSet = selectionSet;
    }

    /// <summary>
    /// Gets the name of the source schema that has requirements. for a field.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the name of the lookup field.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the arguments that represent field requirements.
    /// </summary>
    public ImmutableArray<LookupArgument> Arguments { get; }

    /// <summary>
    /// Gets the paths to the field that are required.
    /// </summary>
    public ImmutableArray<SelectionPath> Fields { get; }

    /// <summary>
    /// Gets the selection set that represents the field requirements.
    /// </summary>
    public SelectionSetNode SelectionSet { get; }

    /// <summary>
    /// Gets the complexity score of fulfilling the requirements.
    /// </summary>
    public int RequirementsCost { get; set; }
}
