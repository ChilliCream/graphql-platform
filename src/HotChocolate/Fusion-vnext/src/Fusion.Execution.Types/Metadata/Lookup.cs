using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a lookup field in a source schema.
/// </summary>
[DebuggerDisplay("{FieldName}:{FieldType} ({SchemaName})")]
public sealed class Lookup : INeedsCompletion
{
    private readonly string _declaringTypeName;
    private readonly string _fieldType;

    /// <summary>
    /// Initializes a new instance of the <see cref="Lookup"/> class.
    /// </summary>
    /// <param name="schemaName">The name of the source schema.</param>
    /// <param name="declaringTypeName">The name of the type that declares the field.</param>
    /// <param name="fieldName">The name of the lookup field.</param>
    /// <param name="fieldType">The type the lookup field returns.</param>
    /// <param name="isInternal">Whether the lookup is internal or not.</param>
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
        string fieldName,
        string fieldType,
        bool isInternal,
        ImmutableArray<LookupArgument> arguments,
        ImmutableArray<IValueSelectionNode> fields)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);
        ArgumentException.ThrowIfNullOrEmpty(declaringTypeName);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);
        ArgumentException.ThrowIfNullOrEmpty(fieldType);

        if (arguments.Length == 0)
        {
            throw new ArgumentException("At least one argument is required.");
        }

        if (fields.Length == 0)
        {
            throw new ArgumentException("At least one field is required.");
        }

        _declaringTypeName = declaringTypeName;
        _fieldType = fieldType;
        SchemaName = schemaName;
        FieldName = fieldName;
        IsInternal = isInternal;
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
    public string FieldName { get; }

    /// <summary>
    /// Gets the type the lookup field returns.
    /// </summary>
    public ITypeDefinition FieldType { get; private set; } = null!;

    /// <summary>
    /// Gets whether the lookup is internal or not.
    /// </summary>
    public bool IsInternal { get; }

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
    {
        Requirements = context.RewriteValueSelectionToSelectionSet(schema, _declaringTypeName, Fields);
        FieldType = schema.Types[_fieldType];
    }
}
