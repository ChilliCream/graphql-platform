using HotChocolate.Language;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// A source schema that is a candidate for resolving an output field, with the field's directive
/// data in that schema.
/// </summary>
internal sealed class FieldOption(
    string schemaName,
    int schemaIndex,
    bool isPartial,
    SelectionSetNode? requirements,
    SelectionSetNode? eventStreamMessage)
{
    /// <summary>
    /// Gets the name of the schema.
    /// </summary>
    public string SchemaName { get; } = schemaName;

    /// <summary>
    /// Gets the index of the schema in the <see cref="SchemaSetTable"/>.
    /// </summary>
    public int SchemaIndex { get; } = schemaIndex;

    /// <summary>
    /// Gets a value indicating whether the field is partial (<c>@external</c>) in the schema.
    /// </summary>
    public bool IsPartial { get; } = isPartial;

    /// <summary>
    /// Gets the requirements of the field in the schema, or <c>null</c> if it has none.
    /// </summary>
    public SelectionSetNode? Requirements { get; } = requirements;

    /// <summary>
    /// Gets the event stream message of the field in the schema, or <c>null</c> if it has none.
    /// </summary>
    public SelectionSetNode? EventStreamMessage { get; } = eventStreamMessage;

    /// <summary>
    /// Gets or sets the object types the field returns in the schema, or <c>null</c> until the
    /// <see cref="FieldOptionTable"/> resolves them.
    /// </summary>
    internal MutableObjectTypeDefinition[]? PossibleTypes { get; set; }
}
