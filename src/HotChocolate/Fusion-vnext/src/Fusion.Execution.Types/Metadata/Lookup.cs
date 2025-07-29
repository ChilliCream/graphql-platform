using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a lookup field in a source schema.
/// </summary>
public sealed class Lookup : INeedsCompletion
{
    private readonly string _declaringTypeName;

    /// <summary>
    /// Initializes a new instance of the <see cref="Lookup"/> class.
    /// </summary>
    /// <param name="schemaName">The name of the source schema.</param>
    /// <param name="declaringTypeName">The name of the type that declares the field.</param>
    /// <param name="name">The name of the lookup field.</param>
    /// <param name="arguments">The arguments that represent field requirements.</param>
    /// <param name="fields">The paths to the field that are required.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="arguments"/> or <paramref name="fields"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="schemaName"/>
    /// or <paramref name="declaringTypeName"/> is null.
    /// </exception>
    public Lookup(
        string schemaName,
        string declaringTypeName,
        string name,
        ImmutableArray<LookupArgument> arguments,
        ImmutableArray<IValueSelectionNode> fields)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);
        ArgumentException.ThrowIfNullOrEmpty(declaringTypeName);
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (arguments.Length == 0)
        {
            throw new ArgumentException("At least one argument is required.");
        }

        if (fields.Length == 0)
        {
            throw new ArgumentException("At least one field is required.");
        }

        _declaringTypeName = declaringTypeName;
        SchemaName = schemaName;
        Name = name;
        Arguments = arguments;
        Fields = fields;
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
    public ImmutableArray<IValueSelectionNode> Fields { get; }

    /// <summary>
    /// Gets the data requirements for this lookup field.
    /// </summary>
    public SelectionSetNode Requirements { get; private set; } = null!;

    void INeedsCompletion.Complete(FusionSchemaDefinition schema, CompositeSchemaBuilderContext context)
        => Requirements = context.RewriteValueSelectionToSelectionSet(schema, _declaringTypeName, Fields);
}
