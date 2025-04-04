using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types;

public sealed class Lookup
{
    public Lookup(
        string schemaName,
        string name,
        ImmutableArray<LookupArgument> arguments,
        ImmutableArray<FieldPath> fields,
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
    public ImmutableArray<FieldPath> Fields { get; }

    public SelectionSetNode SelectionSet { get; }
}
